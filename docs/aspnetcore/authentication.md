[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Authentication and authorization

Callers authenticate with a **JWT bearer token** from one identity provider, and with an **API key** when clients are configured. With both on, every `RequireAuthorization()` accepts either, and the application sees the same thing in both cases: an identity, roles and scopes.

```csharp
services.AddStandardAuthConfiguration(appSettings.IdentityProvider, appSettings.ApiKeys);

app.UseAuthentication().UseAuthorization();      // in your Program.cs, between the two request pipeline steps
```

- **JWT bearer** is always on.
- **API keys** are on when `ApiKeys:Clients` has at least one client. Then a default scheme, `BearerOrApiKey`, looks at the request: the API key header present → API key scheme; otherwise → bearer.

## Identity provider

```json
"IdentityProvider": {
  "AuthorityUrl": "https://auth.example.com/realms/acme",
  "Audience": "api",
  "Provider": "Keycloak",
  "ClockSkewSeconds": 30,
  "RequireHttpsMetadata": true,
  "Scopes": { "api.read": "Read access", "api.write": "Write access" },
  "AuthorizationPath": "protocol/openid-connect/auth",
  "TokenPath": "protocol/openid-connect/token"
}
```

| Key | Default | Meaning |
|---|---|---|
| `AuthorityUrl` | required | The base URL of the identity provider, the token issuer. Must be absolute. |
| `Audience` | required | The audience a token must be issued for. |
| `Provider` | `Keycloak` | `Keycloak` or `Generic`. Decides where **roles** are read from (below). |
| `ClockSkewSeconds` | 30 | How many seconds past its expiry a token is still accepted, to absorb clock drift. Valid 0 to 300. The framework default is 300, which lets expired tokens through for too long. |
| `RequireHttpsMetadata` | `true` | Whether the provider's metadata must be fetched over HTTPS. Turn off only for local development. |
| `Scopes` | `{}` | The scopes the API documentation can request: name to description. Not used to validate tokens. |
| `AuthorizationPath` | Keycloak's | Authorization endpoint, relative to `AuthorityUrl`. Used by the API documentation sign-in. |
| `TokenPath` | Keycloak's | Token endpoint, relative to `AuthorityUrl`. |

`IdentityProviderPaths.Keycloak` and `IdentityProviderPaths.Standard` (`authorize`, `token`) hold common values; OIDC does not standardize the paths, so for another provider copy `authorization_endpoint` and `token_endpoint` from its `/.well-known/openid-configuration`.

**Validation at startup:** the authority must be an absolute URL, `https` when `RequireHttpsMetadata` is true. In **production** a local authority (loopback, `*.localhost`), an `http` authority and `RequireHttpsMetadata: false` are refused.

One provider per service.

## Roles

Whatever the provider writes, the token's roles are normalized at validation into standard role claims (`ClaimTypes.Role`), so endpoints and `User.IsInRole` never depend on the provider.

| `Provider` | Where roles come from |
|---|---|
| `Keycloak` | The **realm roles** (`realm_access.roles`) and the **roles of the client named by `Audience`** (`resource_access.<audience>.roles`). Both are JSON inside one claim. Roles of other clients are not granted. |
| `Generic` | The plain `role` and `roles` claims (Entra, Auth0, …). Keycloak's JSON claims are ignored, so a look-alike claim grants nothing. |

## Scopes

Scopes are read from the space-separated `scope` claim (also `scp` and the URI JwtBearer maps it to), split into individual values and compared exactly (case matters).

## Requiring roles and scopes

On any endpoint or resource group:

```csharp
group.RequireRoles("admin");                    // EVERY role listed
endpoint.RequireAnyRole("admin", "support");    // AT LEAST ONE
endpoint.RequireScopes("api.read", "api.write");// EVERY scope
endpoint.RequireAnyScope("api.read", "api.write");
```

Each call **adds a requirement**; the requirements of every call and every level (group, endpoint) must all pass. The scope methods also require an authenticated user.

In code:

```csharp
user.GetUserId();        // sub of a token, client name of an API key; null when anonymous
user.GetRoles();         // IReadOnlySet<string>
user.GetScopes();
user.HasRole("admin");
user.HasScope("api.read");
```

## API keys

```json
"ApiKeys": {
  "HeaderName": "X-Api-Key",
  "Clients": {
    "billing-service": { "Key": "<secret>", "Roles": ["reader"], "Scopes": ["api.read"] }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `HeaderName` | `X-Api-Key` | The request header that carries the key. Not empty. |
| `Clients` | `{}` | Client name → settings. **The client name becomes the caller's identity** (name and name-identifier claims, and what `GetUserId()` returns). Disabled when empty. |
| `Clients:<name>:Key` | | The secret the client sends. Required. Keys are compared in constant time. |
| `Clients:<name>:Roles` | `[]` | Roles granted to the client. |
| `Clients:<name>:Scopes` | `[]` | Scopes granted to the client. |

**Validation:** keys cannot be empty or shared between clients; in **production** each key must have at least **32 characters** (`ApiKeysOptionsValidator.MinimumProductionKeyLength`). Supply real keys through the environment: `ApiKeys__Clients__billing-service__Key=…`. Only local values belong in `appsettings.json`.

## Pinning a scheme

By default an endpoint accepts both. To accept only one:

```csharp
endpoint.RequireAuthorization(policy => policy
    .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)      // or JwtBearerDefaults.AuthenticationScheme
    .RequireAuthenticatedUser());
```

The OpenAPI document follows ([OpenAPI](../openapi/README.md)): an endpoint pinned to the API key lists only the API key, one pinned to bearer lists the token schemes, and one that pins nothing lists all of them.

## Anonymous and authenticated by default

Resource groups require an authenticated caller **by default**, so a new endpoint fails closed. Opt out with `RequireAuthorization => false` on the group or `.AllowAnonymous()` on the endpoint ([minimal endpoints](./minimal-endpoints.md#authorization)).

## Errors

A missing or invalid credential is answered `401`, a caller without the required role or scope `403`, both with a problem body ([results to HTTP](./results-to-http.md)).
