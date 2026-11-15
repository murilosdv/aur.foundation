using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.OpenApi.Extensions;
using Aurore.Foundation.Tests.OpenApi.Support;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Aurore.Foundation.Tests.OpenApi.Documents;

[Trait("Feature", "OpenAPI documents")]
public sealed class OpenApiDocumentTests(DocumentsApp app) : IClassFixture<DocumentsApp>
{
    public static TheoryData<int, string> DocumentNames => new()
    {
        { 1, "v1" },
        { 2, "v2" },
        { 10, "v10" },
    };

    [Theory(DisplayName = "A document is named after the version, in the format of the API explorer groups")]
    [MemberData(nameof(DocumentNames))]
    public void DocumentNameFollowsTheVersion(int version, string expected)
    {
        // Arrange & Act
        var name = OpenApiExtensions.DocumentName(version);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact(DisplayName = "Each API version has its own document, and unknown versions have none")]
    public async Task EachVersionHasItsOwnDocument()
    {
        // Arrange & Act
        var v1 = await app.Client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var v2 = await app.Client.GetAsync("/openapi/v2.json", TestContext.Current.CancellationToken);
        var v3 = await app.Client.GetAsync("/openapi/v3.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, v1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, v3.StatusCode);
    }

    [Fact(DisplayName = "The document carries the title, version, description and contact of the application")]
    public async Task DocumentCarriesTheApplicationInformation()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var info = document.RootElement.GetProperty("info");

        // Assert
        Assert.Equal("Orders Service", info.GetProperty("title").GetString());
        Assert.Equal("1.0", info.GetProperty("version").GetString());
        Assert.Equal("Takes orders.", info.GetProperty("description").GetString());
        Assert.Equal("Orders Team", info.GetProperty("contact").GetProperty("name").GetString());
        Assert.Equal("orders@example.com", info.GetProperty("contact").GetProperty("email").GetString());
    }

    [Fact(DisplayName = "A deprecated version says so with its sunset date, after the description of the version")]
    public async Task DeprecatedVersionSaysSo()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v2");

        // Act
        var description = document.RootElement.GetProperty("info").GetProperty("description").GetString();

        // Assert
        Assert.Equal("Takes orders.\n\nThe second version.\n\n**Deprecated.** This version stops being served on 2027-01-01.", description);
    }

    [Fact(DisplayName = "The four security schemes are documented in the components")]
    public async Task SecuritySchemesAreDocumented()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var schemes = document.RootElement.GetProperty("components").GetProperty("securitySchemes").EnumerateObject().Select(x => x.Name).Order().ToArray();

        // Assert
        Assert.Equal(["Bearer", "OAuth2", "OIDC", "X-Api-Key"], schemes);
    }

    [Fact(DisplayName = "The OAuth2 and OIDC schemes point at the identity provider")]
    public async Task OAuth2AndOidcPointAtTheIdentityProvider()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");
        var schemes = document.RootElement.GetProperty("components").GetProperty("securitySchemes");

        // Act
        var oidc = schemes.GetProperty("OIDC").GetProperty("openIdConnectUrl").GetString();
        var flow = schemes.GetProperty("OAuth2").GetProperty("flows").GetProperty("authorizationCode");

        // Assert
        Assert.Equal("https://idp.example.com/realms/test/.well-known/openid-configuration", oidc);
        Assert.Equal("https://idp.example.com/realms/test/protocol/openid-connect/auth", flow.GetProperty("authorizationUrl").GetString());
        Assert.Equal("https://idp.example.com/realms/test/protocol/openid-connect/token", flow.GetProperty("tokenUrl").GetString());
        Assert.True(flow.GetProperty("scopes").TryGetProperty("api.read", out _));
    }

    public static TheoryData<string, bool> OperationSecurity => new()
    {
        { "/v1/open", false },
        { "/v1/locked", true },
        { "/v1/group/locked", true }, // the group requires authorization
        { "/v1/group/open", false }, // the endpoint allows anonymous inside a locked group
    };

    [Theory(DisplayName = "An operation lists security schemes only when it requires authorization")]
    [MemberData(nameof(OperationSecurity))]
    public async Task OperationSecurityFollowsTheAuthorizationMetadata(string path, bool expectedSecured)
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get");
        var secured = operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0;

        // Assert
        Assert.Equal(expectedSecured, secured);
    }

    public static TheoryData<string, string[]> SchemesByEndpoint => new()
    {
        { "/v1/either", ["Bearer", "OAuth2", "OIDC", "X-Api-Key"] }, // nothing pinned: every scheme
        { "/v1/api-key-only", ["X-Api-Key"] },
        { "/v1/bearer-only", ["Bearer", "OAuth2", "OIDC"] },
    };

    [Theory(DisplayName = "An operation lists only the security schemes its endpoint accepts")]
    [MemberData(nameof(SchemesByEndpoint))]
    public async Task OperationListsOnlyTheSchemesItAccepts(string path, string[] expected)
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var schemes = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("security")
            .EnumerateArray()
            .SelectMany(requirement => requirement.EnumerateObject().Select(x => x.Name))
            .Order()
            .ToArray();

        // Assert
        Assert.Equal(expected, schemes);
    }

    [Fact(DisplayName = "The document does not apply security to every operation")]
    public async Task DocumentHasNoGlobalSecurity()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var hasGlobalSecurity = document.RootElement.TryGetProperty("security", out _);

        // Assert
        Assert.False(hasGlobalSecurity);
    }

    [Fact(DisplayName = "Query parameters are documented in camel case like the JSON, and headers keep their names")]
    public async Task QueryParametersAreDocumentedInCamelCase()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var parameters = document.RootElement
            .GetProperty("paths").GetProperty("/v1/people").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .Select(x => (Name: x.GetProperty("name").GetString(), In: x.GetProperty("in").GetString()))
            .ToList();

        // Assert
        Assert.Contains(("pageSize", "query"), parameters);
        Assert.Contains(("orderBy", "query"), parameters);
        Assert.Contains(("createdFrom", "query"), parameters);
        Assert.DoesNotContain(parameters, x => x.In == "query" && char.IsUpper(x.Name![0]));
        Assert.Contains(("Correlation-Id", "header"), parameters);
    }

    [Fact(DisplayName = "Every operation documents the optional Correlation-Id header")]
    public async Task EveryOperationDocumentsTheCorrelationId()
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var operations = document.RootElement.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).ToList();
        var withHeader = operations.Count(operation => operation.Value.GetProperty("parameters").EnumerateArray()
            .Any(x => x.GetProperty("name").GetString() == "Correlation-Id" && !(x.TryGetProperty("required", out var required) && required.GetBoolean())));

        // Assert
        Assert.NotEmpty(operations);
        Assert.Equal(operations.Count, withHeader);
    }

    public static TheoryData<string, string, bool?> IdempotencyKeyHeaders => new()
    {
        { "/v1/orders", "post", false }, // WithIdempotency(): optional
        { "/v1/payments", "post", true }, // WithIdempotency(required: true)
        { "/v1/people", "get", null }, // not idempotent: no header
        { "/v1/notes", "post", null }, // a POST without the mark: no header
    };

    [Theory(DisplayName = "The Idempotency-Key header is documented only where the endpoint uses it, as required only when it must be sent")]
    [MemberData(nameof(IdempotencyKeyHeaders))]
    public async Task IdempotencyKeyHeaderFollowsTheEndpointMetadata(string path, string method, bool? expectedRequired)
    {
        // Arrange
        using var document = await app.GetDocumentAsync("v1");

        // Act
        var header = document.RootElement
            .GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("parameters")
            .EnumerateArray()
            .Where(x => x.GetProperty("name").GetString() == "Idempotency-Key")
            .Select(x => (bool?)(x.TryGetProperty("required", out var required) && required.GetBoolean()))
            .SingleOrDefault();

        // Assert
        Assert.Equal(expectedRequired, header);
    }

    [Fact(DisplayName = "A security scheme without a name is refused")]
    public void SecuritySchemeWithoutANameIsRefused()
    {
        // Arrange
        var options = new OpenApiOptions();
        var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http };

        // Act
        var act = () => options.AddStandardSecuritySchemeAndRequirement("Bearer", scheme);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact(DisplayName = "Without API key clients the API key is not documented")]
    public void ApiKeyWithoutClientsIsNotDocumented()
    {
        // Arrange
        var options = new OpenApiOptions();

        // Act
        var result = options.AddStandardApiKeySecurity(new ApiKeysOptions());

        // Assert
        Assert.Same(options, result);
    }
}
