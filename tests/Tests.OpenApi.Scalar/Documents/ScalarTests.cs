using System.Collections.Generic;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.OpenApi.Extensions;
using Aurore.Foundation.OpenApi.Scalar.Extensions;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Aurore.Foundation.Tests.OpenApi.Scalar.Documents;

[Trait("Feature", "Scalar")]
public sealed class ScalarTests
{
    private static readonly IdentityProviderOptions _identity = new()
    {
        AuthorityUrl = "https://idp.example.com/realms/test",
        Audience = "api",
        Scopes = new Dictionary<string, string> { ["api.read"] = "Read" }
    };

    private static readonly MinimalEndpointVersion[] _versions = [1, new(2, true)];

    [Fact(DisplayName = "The page is preconfigured for OAuth2 sign-in with PKCE and the client id of the documentation")]
    public async Task PageCarriesTheOAuth2Configuration()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions { OAuthClientId = "api-docs" });

        // Act
        var html = await app.Client.GetStringAsync("/scalar/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("api-docs", html);
        Assert.Contains("OAuth2", html);
        Assert.Contains("SHA-256", html);
    }

    [Fact(DisplayName = "Without a client id the page carries no sign-in configuration")]
    public async Task WithoutAClientIdThereIsNoSignIn()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions());

        // Act
        var html = await app.Client.GetStringAsync("/scalar/", TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain("SHA-256", html);
    }

    [Fact(DisplayName = "The page lists one document per version, and marks the deprecated ones")]
    public async Task PageListsTheVersions()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions());

        // Act
        var html = await app.Client.GetStringAsync("/scalar/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Version 1", html);
        Assert.Contains("Version 2 (deprecated)", html);
        Assert.Contains("openapi/v1.json", html);
        Assert.Contains("openapi/v2.json", html);
    }

    private static Task<TestApp> StartAsync(ApiDocsOptions apiDocs)
    {
        return TestApp.StartAsync(
            builder =>
            {
                builder.ConfigureStandardScalarDocuments(_versions);

                foreach (var version in _versions)
                    builder.Services.AddOpenApi(OpenApiExtensions.DocumentName(version.Number));
            },
            app =>
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options => options.WithStandardAuthentication(_identity, apiDocs));
            });
    }
}
