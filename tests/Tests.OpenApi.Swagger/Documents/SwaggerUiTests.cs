using System.Collections.Generic;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.OpenApi.Extensions;
using Aurore.Foundation.OpenApi.Swagger.Extensions;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.OpenApi.Swagger.Documents;

[Trait("Feature", "Swagger UI")]
public sealed class SwaggerUiTests
{
    private static readonly IdentityProviderOptions _identity = new()
    {
        AuthorityUrl = "https://idp.example.com/realms/test",
        Audience = "api",
        Scopes = new Dictionary<string, string> { ["api.read"] = "Read" }
    };

    private static readonly MinimalEndpointVersion[] _versions = [1, new(2, true)];

    [Fact(DisplayName = "The selector lists one document per version, in order, and marks the deprecated ones")]
    public async Task SelectorListsTheVersions()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions());

        // Act
        var configuration = await GetConfigurationAsync(app);

        // Assert
        var first = configuration.IndexOf("../openapi/v1.json", System.StringComparison.Ordinal);
        var second = configuration.IndexOf("../openapi/v2.json", System.StringComparison.Ordinal);
        Assert.True(first >= 0 && second > first);
        Assert.Contains("Version 1", configuration);
        Assert.Contains("Version 2 (deprecated)", configuration);
    }

    [Fact(DisplayName = "Sign-in is preconfigured for OAuth2 with PKCE and the client id of the documentation")]
    public async Task SignInIsPreconfigured()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions { OAuthClientId = "api-docs" });

        // Act
        var configuration = await GetConfigurationAsync(app);

        // Assert
        Assert.Contains("api-docs", configuration);
        Assert.Matches(@"""usePkceWithAuthorizationCodeGrant""\s*:\s*true", configuration);
        Assert.Contains("api.read", configuration);
    }

    [Fact(DisplayName = "Without a client id nothing is preconfigured")]
    public async Task WithoutAClientIdNothingIsPreconfigured()
    {
        // Arrange
        await using var app = await StartAsync(new ApiDocsOptions());

        // Act
        var configuration = await GetConfigurationAsync(app);

        // Assert
        Assert.DoesNotMatch(@"""usePkceWithAuthorizationCodeGrant""\s*:\s*true", configuration);
    }

    private static async Task<string> GetConfigurationAsync(TestApp app)
    {
        var page = await app.Client.GetStringAsync("/swagger/index.html", TestContext.Current.CancellationToken);
        var script = await app.Client.GetStringAsync("/swagger/index.js", TestContext.Current.CancellationToken);

        return page + script;
    }

    private static Task<TestApp> StartAsync(ApiDocsOptions apiDocs)
    {
        return TestApp.StartAsync(
            builder =>
            {
                foreach (var version in _versions)
                    builder.Services.AddOpenApi(OpenApiExtensions.DocumentName(version.Number));
            },
            app =>
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options => options
                    .WithStandardDocuments(_versions)
                    .WithStandardAuthentication(_identity, apiDocs));
            });
    }
}
