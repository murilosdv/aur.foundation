using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.OpenApi.Extensions;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.OpenApi.Support;

/// <summary>
/// Endpoints for every case the documents describe, with every transformer of the package.
/// </summary>
public sealed class DocumentsApp : IAsyncLifetime
{
    private TestApp _app = null!;

    public static MinimalEndpointVersion[] Versions { get; } = [1, new(2, true, new DateOnly(2027, 1, 1), "The second version.")];

    public static AppInformationOptions AppInfo { get; } = new()
    {
        Component = "orders-service",
        Domain = "sales",
        Boundary = "orders",
        Description = "Takes orders.",
        Maintainer = new AppMaintainerOptions("Orders Team", "orders@example.com")
    };

    public static IdentityProviderOptions Identity { get; } = new()
    {
        AuthorityUrl = "https://idp.example.com/realms/test/",
        Audience = "api",
        Scopes = new Dictionary<string, string> { ["api.read"] = "Read" }
    };

    public static ApiKeysOptions ApiKeys { get; } = new()
    {
        Clients = new Dictionary<string, ApiKeyClientOptions> { ["billing"] = new() { Key = "secret" } }
    };

    public HttpClient Client => _app.Client;

    public async Task<JsonDocument> GetDocumentAsync(string name)
    {
        var json = await Client.GetStringAsync($"/openapi/{name}.json", TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }

    public async ValueTask InitializeAsync()
    {
        _app = await TestApp.StartAsync(
            builder =>
            {
                foreach (var version in Versions)
                {
                    builder.Services.AddOpenApi(OpenApiExtensions.DocumentName(version.Number), options => options
                        .AddStandardDocumentInfo(version, AppInfo)
                        .AddStandardJwtBearerSecurity()
                        .AddStandardApiKeySecurity(ApiKeys)
                        .AddStandardOidcSecurity(Identity)
                        .AddStandardOAuth2Security(Identity)
                        .AddStandardCamelCaseQueryParameters()
                        .AddStandardCorrelationIdHeader()
                        .AddStandardIdempotencyKeyHeader());
                }
            },
            app =>
            {
                app.MapOpenApi();

                app.MapGet("/v1/open", () => "open");
                app.MapGet("/v1/locked", () => "locked").RequireAuthorization();
                app.MapGet("/v1/either", () => "either").RequireAuthorization();
                app.MapGet("/v1/api-key-only", () => "key").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "ApiKey" });
                app.MapGet("/v1/bearer-only", () => "bearer").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme });
                app.MapGet("/v1/people", (int pageSize, string? orderBy, string? createdFrom) => "people");

                var locked = app.MapGroup("/v1/group").RequireAuthorization();
                locked.MapGet("/locked", () => "locked");
                locked.MapGet("/open", () => "open").AllowAnonymous();

                app.MapPost("/v1/orders", () => "order").WithIdempotency();
                app.MapPost("/v1/payments", () => "payment").WithIdempotency(required: true);
                app.MapPost("/v1/notes", () => "note");
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }
}
