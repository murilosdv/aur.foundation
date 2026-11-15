[Home](../../README.md) · [Documentation](../README.md) · [OpenAPI](./README.md)

# Swagger UI

`Aurore.Foundation.OpenApi.Swagger` puts Swagger UI (only the interface, from `Swashbuckle.AspNetCore.SwaggerUI`; the document generator is ASP.NET Core's) on the [OpenAPI documents](./README.md) and adds two things specific to it.

```
dotnet add package Aurore.Foundation.OpenApi.Swagger
```

```csharp
app.MapOpenApi();
app.UseSwaggerUI(options => options
    .WithStandardDocuments(endpoints.Versions)
    .WithStandardAuthentication(appSettings.IdentityProvider, appSettings.ApiDocs));
```

| Method | Does |
|---|---|
| `WithStandardDocuments(versions)` | Lists the catalog versions in the selector, in the order given (the first is the default), and marks the deprecated ones. Each entry points to `../openapi/{version}.json`, so **the page must live next to the `/openapi` route**; the default `/swagger` does. |
| `WithStandardAuthentication(identityProvider, apiDocs)` | Pre-configures the **Authorize** button for the OAuth2 authorization code flow with **PKCE**, with the client id from `ApiDocs:OAuthClientId` and the scopes from `IdentityProvider:Scopes`. Does nothing when the client id is empty. |

## Setting up the sign-in

Create a **public** client with the authorization code flow and PKCE at the identity provider, add the page's redirect URI (Swagger UI uses its `oauth2-redirect.html` next to the page), and set `ApiDocs:OAuthClientId`. Steps and reasoning are the same as for [Scalar](./scalar.md#setting-up-the-sign-in).

Like Scalar, the interface is meant to run outside production.
