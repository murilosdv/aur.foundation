using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Handlers;
using Aurore.Foundation.AspNetCore.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Tests.AspNetCore.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.AspNetCore.Authentication.Handlers;

[Trait("Feature", "API key authentication")]
public sealed class ApiKeyAuthenticationHandlerTests
{
    private const string Header = "X-Test-Key";

    private static readonly IReadOnlyDictionary<string, ApiKeyClientOptions> _clients = new Dictionary<string, ApiKeyClientOptions>
    {
        ["billing"] = new() { Key = "billing-key-0123456789", Roles = ["reader", "editor"], Scopes = ["api.read", "api.write"] },
        ["reporting"] = new() { Key = "reporting-key-0123456789", Roles = ["reader"], Scopes = ["api.read"] },
    };

    [Fact(DisplayName = "The client whose key is presented becomes the caller, with its roles and scopes")]
    public async Task MatchingKeyAuthenticatesTheClient()
    {
        // Arrange
        var http = Context("reporting-key-0123456789");

        // Act
        var result = await http.AuthenticateAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("reporting", result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("reporting", result.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(["reader"], result.Principal.FindAll(ClaimTypes.Role).Select(x => x.Value));
        Assert.Equal("api.read", result.Principal.FindFirstValue("scope"));
    }

    [Fact(DisplayName = "The scopes of a client become one space-separated scope claim, and its roles one claim each")]
    public async Task RolesAndScopesBecomeClaims()
    {
        // Arrange
        var http = Context("billing-key-0123456789");

        // Act
        var result = await http.AuthenticateAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.Equal(["reader", "editor"], result.Principal!.FindAll(ClaimTypes.Role).Select(x => x.Value));
        Assert.Equal("api.read api.write", result.Principal.FindFirstValue("scope"));
    }

    [Fact(DisplayName = "A key that belongs to no client fails the authentication")]
    public async Task UnknownKeyFails()
    {
        // Arrange
        var http = Context("not-a-key-at-all-0123456789");

        // Act
        var result = await http.AuthenticateAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact(DisplayName = "A request without the key header is left to the other schemes, not failed")]
    public async Task MissingKeyHasNoResult()
    {
        // Arrange
        var http = Context(key: null);

        // Act
        var result = await http.AuthenticateAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact(DisplayName = "A key is only looked for in the configured header")]
    public async Task OtherHeaderIsIgnored()
    {
        // Arrange
        var http = Context(key: null);
        http.Request.Headers["X-Api-Key"] = "reporting-key-0123456789";

        // Act
        var result = await http.AuthenticateAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact(DisplayName = "A challenge answers 401 and names the header the key goes in")]
    public async Task ChallengeAnswers401WithTheHeader()
    {
        // Arrange
        var http = Context(key: null);

        // Act
        await http.ChallengeAsync(ApiKeyDefaults.AuthenticationScheme);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
        Assert.Equal($"{ApiKeyDefaults.AuthenticationScheme} header=\"{Header}\"", http.Response.Headers.WWWAuthenticate.ToString());
    }

    private static DefaultHttpContext Context(string? key)
    {
        var http = HttpContextFactory.Create(configure: services => services
            .AddAuthentication()
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, options =>
            {
                options.HeaderName = Header;
                options.Clients = _clients;
            }));

        if (key is not null)
            http.Request.Headers[Header] = key;

        return http;
    }
}
