using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Configuration;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Aurore.Foundation.Tests.AspNetCore.Authentication;

/// <summary>
/// Bearer tokens signed with a local key against the registered authentication, so no identity provider is needed.
/// </summary>
[Trait("Feature", "JWT authentication")]
public sealed class JwtAuthenticationTests : IAsyncLifetime
{
    private const string Issuer = "https://idp.example.com/realms/test";

    private static readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes("a-local-key-only-for-these-tests-0123456789"));

    private TestApp _app = null!;

    public async ValueTask InitializeAsync()
    {
        var identity = new IdentityProviderOptions { AuthorityUrl = Issuer, Audience = "api" };

        _app = await TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddStandardAuthConfiguration(identity, new ApiKeysOptions());
                builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    options.TokenValidationParameters.IssuerSigningKey = _key;
                    options.TokenValidationParameters.ValidIssuer = Issuer;
                });
            },
            app =>
            {
                app.UseAuthentication().UseAuthorization();

                app.MapGet("/authenticated", () => "ok").RequireAuthorization();
                app.MapGet("/scope", () => "ok").RequireScopes("api.read");
                app.MapGet("/role", () => "ok").RequireRoles("admin");
                app.MapGet("/me", (ClaimsPrincipal user) => new
                {
                    Subject = user.GetUserId(),
                    Roles = user.GetRoles(),
                    Scopes = user.GetScopes()
                }).RequireAuthorization();
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    public static TheoryData<string, HttpStatusCode> Access => new()
    {
        { "/authenticated", HttpStatusCode.OK }, // any valid token
        { "/scope", HttpStatusCode.OK }, // the token has api.read
        { "/role", HttpStatusCode.Forbidden }, // the token has no admin role
    };

    [Theory(DisplayName = "A valid token opens the endpoints its roles and scopes allow, and no others")]
    [MemberData(nameof(Access))]
    public async Task TokenOpensWhatItsRolesAndScopesAllow(string path, HttpStatusCode expected)
    {
        // Arrange
        using var client = ClientWith(Token());

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact(DisplayName = "The admin role from the realm opens the endpoint that needs it")]
    public async Task RealmRoleOpensTheRoleEndpoint()
    {
        // Arrange
        using var client = ClientWith(Token(realmRoles: ["admin"]));

        // Act
        using var response = await client.GetAsync("/role", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Without a token the endpoint answers 401")]
    public async Task WithoutATokenTheAnswerIs401()
    {
        // Arrange & Act
        using var response = await _app.Client.GetAsync("/authenticated", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> RefusedTokens => new()
    {
        { "tampered", Token()[..^4] + "AAAA" },
        { "wrong audience", Token(audience: "another-api") },
        { "wrong issuer", Token(issuer: "https://evil.example.com") },
        { "expired", Token(expires: DateTime.UtcNow.AddHours(-1), notBefore: DateTime.UtcNow.AddHours(-2)) },
        { "signed with another key", Token(key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes("another-key-than-the-one-of-the-server"))) },
    };

    [Theory(DisplayName = "A token that is tampered, for another audience or issuer, expired or signed by another key is refused")]
    [MemberData(nameof(RefusedTokens))]
    public async Task InvalidTokensAreRefused(string reason, string token)
    {
        // Arrange
        using var client = ClientWith(token);

        // Act
        using var response = await client.GetAsync("/authenticated", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized, reason);
    }

    [Fact(DisplayName = "Realm roles and the roles of the API client both become roles, and the scope claim becomes the scopes")]
    public async Task RolesAndScopesBecomeThoseOfTheCaller()
    {
        // Arrange
        using var client = ClientWith(Token(realmRoles: ["reader"], clientRoles: ["editor"], scope: "api.read api.write"));

        // Act
        var me = await client.GetFromJsonAsync<Me>("/me", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(me);
        Assert.Equal(["editor", "reader"], me.Roles.Order());
        Assert.Equal(["api.read", "api.write"], me.Scopes.Order());
    }

    [Fact(DisplayName = "The subject of the token is the id of the caller")]
    public async Task SubjectIsTheUserId()
    {
        // Arrange
        using var client = ClientWith(Token(subject: "user-42"));

        // Act
        var me = await client.GetFromJsonAsync<Me>("/me", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("user-42", me?.Subject);
    }

    private HttpClient ClientWith(string token)
    {
        var client = _app.Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static string Token(
        string subject = "user-1",
        string audience = "api",
        string issuer = Issuer,
        string scope = "api.read",
        string[]? realmRoles = null,
        string[]? clientRoles = null,
        SecurityKey? key = null,
        DateTime? expires = null,
        DateTime? notBefore = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(10),
            NotBefore = notBefore,
            SigningCredentials = new SigningCredentials(key ?? _key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["scope"] = scope,
                ["realm_access"] = new Dictionary<string, object> { ["roles"] = realmRoles ?? [] },
                ["resource_access"] = new Dictionary<string, object> { ["api"] = new Dictionary<string, object> { ["roles"] = clientRoles ?? [] } },
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed record Me(string Subject, string[] Roles, string[] Scopes);
}
