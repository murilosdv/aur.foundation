[Home](../../README.md) · [Documentation](../README.md) · [OpenAPI](./README.md)

# Scalar

`Aurore.Foundation.OpenApi.Scalar` puts the [Scalar](https://scalar.com) reference UI on the [OpenAPI documents](./README.md) and adds two things specific to it.

```
dotnet add package Aurore.Foundation.OpenApi.Scalar
```

```csharp
builder.ConfigureStandardScalarDocuments(endpoints.Versions);

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle(appSettings.AppInformation.Component.ToTitleCase())
    .WithStandardAuthentication(appSettings.IdentityProvider, appSettings.ApiDocs));
```

| Method | Does |
|---|---|
| `ConfigureStandardScalarDocuments(versions)` | Registers **one Scalar document per catalog version**, in the order given (the first is the default), and marks the deprecated ones. |
| `WithStandardAuthentication(identityProvider, apiDocs)` | Pre-configures the **Authorize** button for the OAuth2 authorization code flow with **PKCE (SHA-256)**, so signing in from the page redirects to the identity provider without pasting anything. Uses `ApiDocs:OAuthClientId` as the client and `IdentityProvider:Scopes` as the scopes. Does nothing when the client id is empty. |

Theme, title and default HTTP client are set by the application when it maps the page.

## Setting up the sign-in

1. In the identity provider create a **public** client (no secret) with the authorization code flow and PKCE.
2. Add the page URL (for example `https://api.example.com/scalar/`) as a valid redirect URI, and the page origin as a web origin.
3. Put the client id in `ApiDocs:OAuthClientId`.
4. Make sure the scopes you want to offer are in `IdentityProvider:Scopes` and allowed for that client.

Without a client id nothing is configured; a bearer token can still be pasted by hand, as long as the bearer scheme is in the document.

## Production

The interactive documentation is meant to run **outside production** (`ApiDocs` is described as such), and the archetype does not map it there.
