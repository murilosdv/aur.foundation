[Home](../../README.md) · [Documentation](../README.md)

# OpenAPI

`Aurore.Foundation.OpenApi` builds the application's OpenAPI documents on top of ASP.NET Core OpenAPI (`AddOpenApi`): **one document per API version**, with the service's information, its security schemes and the headers the platform defines. It has **no UI**: add the package of the interface you want.

```
dotnet add package Aurore.Foundation.OpenApi
```

| UI package | Interface | Guide |
|---|---|---|
| `Aurore.Foundation.OpenApi.Scalar` | Scalar | [Scalar](./scalar.md) |
| `Aurore.Foundation.OpenApi.Swagger` | Swagger UI | [Swagger](./swagger.md) |

The documents are identical with both (or with none: the JSON alone is what client generators read). Whether they exist at all is up to the application; the archetype does not register them in Production.

## Wiring

Register one document per version in the [catalog](../aspnetcore/minimal-endpoints.md), with the transformers you want, and map the documents:

```csharp
foreach (var version in endpoints.Versions)
{
    builder.Services.AddOpenApi(OpenApiExtensions.DocumentName(version.Number), options => options
        .AddStandardDocumentInfo(version, appSettings.AppInformation)
        .AddStandardJwtBearerSecurity()
        .AddStandardApiKeySecurity(appSettings.ApiKeys)
        .AddStandardOidcSecurity(appSettings.IdentityProvider)
        .AddStandardOAuth2Security(appSettings.IdentityProvider)
        .AddStandardCamelCaseQueryParameters()
        .AddStandardCorrelationIdHeader()
        .AddStandardIdempotencyKeyHeader());
}

app.MapOpenApi();      // /openapi/v1.json, /openapi/v2.json, …
```

ASP.NET Core OpenAPI generates code that reads the endpoints' XML comments, and the compiler refuses it (error `CS9137`) unless the **application's** project allows its namespace:

```xml
<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>
```

## The transformers

| Method | Adds |
|---|---|
| `AddStandardDocumentInfo(version, appInfo)` | Title (the Component in title case), version (`1.0` for v1), description and the maintainer's contact (name, email, URL) from `AppInformation`. The description joins the service description, the version's own `Description` and, for a deprecated version, "**Deprecated.**" with the sunset date when there is one. |
| `AddStandardJwtBearerSecurity()` | A `Bearer` scheme: paste a token into the interface. |
| `AddStandardApiKeySecurity(apiKeys)` | An API key scheme on the configured header (`ApiKeys:HeaderName`). The scheme is listed under the header name too, because OpenAPI uses `name` as the header name. |
| `AddStandardOidcSecurity(identityProvider)` | OpenID Connect against `IdentityProvider:AuthorityUrl`. |
| `AddStandardOAuth2Security(identityProvider)` | The OAuth2 authorization code flow, with the provider's authorization and token endpoints and the `Scopes` listed in settings. |
| `AddStandardSecuritySchemeAndRequirement(name, scheme, scopes…)` | The building block for any other scheme. |
| `AddStandardCamelCaseQueryParameters()` | Query parameter names in camelCase (`pageSize`, not `PageSize`), like the JSON bodies. The generator uses the C# property names, which the JSON naming policy never touches. Binding ignores case, so only the document changes. Headers and route values are left alone. |
| `AddStandardCorrelationIdHeader()` | The optional `Correlation-Id` request header on every operation. |
| `AddStandardIdempotencyKeyHeader()` | The `Idempotency-Key` request header, **only on operations marked `WithIdempotency()`**; required when marked `required: true`. |

Add only the schemes your service accepts: a service without API keys leaves out `AddStandardApiKeySecurity`.

## Security per operation

The padlock is decided **per operation** from the endpoint's metadata:

- authorized endpoints are locked, `AllowAnonymous` ones are not;
- an endpoint [pinned](../aspnetcore/authentication.md#pinning-a-scheme) to one authentication scheme lists only the schemes that accept it (pinned to the API key: only the API key; pinned to bearer: the token schemes);
- an endpoint that pins nothing lists every scheme.

## Names

`OpenApiExtensions.DocumentName(version)` returns `v1`, `v2`, …, built with the same format the API explorer uses for its groups (`GroupNameFormat`), so a document always matches the endpoints of its version and the two cannot drift.

## Describing endpoints

The interface shows what the endpoint says about itself:

- `Summary` and `Description` of the endpoint class, or `.WithSummary(…)` / `.WithDescription(…)` in its `Map`, which win ([order](../aspnetcore/minimal-endpoints.md#name-summary-and-description-the-last-one-wins));
- `.Produces<T>()`, `.ProducesPage<T>()`, `.ProducesCursorPage<T>()`, `.ProducesProblem(404)`;
- `[Description("…")]` on request properties (the pagination requests already have them);
- XML comments, through the interceptor above.

The standard problems (401, 403, 429, 500) and the 400 validation problem are already on every operation ([minimal endpoints](../aspnetcore/minimal-endpoints.md#documented-problems)).
