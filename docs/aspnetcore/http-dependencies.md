[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# HTTP dependencies

The services this one calls. Each is **declared once in settings and registered once in code**. The client you get carries the base address, default headers, the current request's correlation id, the standard resilience pipeline and, when asked, authentication.

```csharp
// Named client
services.AddStandardHttpDependency("billing", appSettings.HttpDependencies.GetRequired("billing"));

// Typed client: IBillingClient receives the configured HttpClient
services.AddStandardHttpDependency<IBillingClient, BillingClient>("billing", appSettings.HttpDependencies.GetRequired("billing"));
```

Resolve a named client with `IHttpClientFactory.CreateClient("billing")`; a typed one is injected as its interface. `GetRequired(name)` returns the dependency from the settings dictionary or throws naming the missing key.

## Settings

`HttpDependencies` is a dictionary: dependency name → settings.

```json
"HttpDependencies": {
  "billing": {
    "BaseUrl": "https://billing.internal.example.com",
    "RequestHeaders": { "Accept": "application/json" },
    "PropagateCorrelationId": true,
    "ApiKey": { "HeaderName": "X-Api-Key", "Value": "<secret>" },
    "ClientCredentials": { "TokenUrl": "https://auth.example.com/realms/acme/protocol/openid-connect/token", "ClientId": "invoice-api", "ClientSecret": "<secret>", "Scopes": ["billing.read"] },
    "HealthCheck": { "ReadinessEndpoint": "/health/ready", "TimeoutSeconds": 5, "Critical": true, "Tags": [] }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `BaseUrl` | required | The base URL of the upstream service. Absolute; **a local address is refused in production**. |
| `RequestHeaders` | `{}` | Headers sent with every request: `Accept`, `User-Agent`, API-version headers. |
| `PropagateCorrelationId` | `true` | Forwards the current `Correlation-Id` to the upstream. Turn off for third-party APIs that should not see internal ids. |
| `ApiKey` | none | A static header, for upstreams that authenticate by key. Holds values only; see below. |
| `ClientCredentials` | none | OAuth2 client credentials, for upstreams that authenticate by token. |
| `HealthCheck` | none | When present, the upstream is part of the readiness checks. |

The authentication sections **only hold values**: the application chooses which one to apply when it registers the client, so one dependency block can be reused.

### `ApiKey`

| Key | Default | Meaning |
|---|---|---|
| `HeaderName` | `X-Api-Key` | The header that carries the value (can be `Authorization` for a static bearer token). Not empty. |
| `Value` | empty | The key or token. Required. **Supply it through an environment variable or a secret store.** |

### `ClientCredentials`

| Key | Meaning |
|---|---|
| `TokenUrl` | The OAuth2 token endpoint (Keycloak: `{authority}/protocol/openid-connect/token`). Absolute; local refused in production. |
| `ClientId` | The client registered at the identity provider. Required. |
| `ClientSecret` | The client secret. Required. **Environment variable or secret store.** |
| `Scopes` | The scopes to request. |

### `HealthCheck`

| Key | Default | Meaning |
|---|---|---|
| `ReadinessEndpoint` | `/health/ready` | The path called on the upstream, relative to its base URL. |
| `TimeoutSeconds` | 5 | How long the call may take before the check fails. Greater than 0. |
| `Critical` | `false` | A critical dependency makes this service Unhealthy (503) when it fails; others only Degraded (200). |
| `Tags` | `[]` | Extra tags added to the health check. |

See [health checks](./health-checks.md).

## What every client gets

- `BaseAddress` and the default `RequestHeaders`.
- **Correlation id propagation**: the `Correlation-Id` of the request being handled is added to the outgoing call (nothing is added outside a request, such as in background work).
- The **standard resilience pipeline** (`AddStandardResilienceHandler`: rate limiter, total timeout, retry with backoff, circuit breaker, attempt timeout), so a slow or failing upstream does not take this service down with it.
- Telemetry: HTTP client spans and metrics ([telemetry](../telemetry/README.md)).

## Authenticating

Chain one of these on the registration. Without any, the client is anonymous.

```csharp
// Static header
services.AddStandardHttpDependency("inventory", inventory)
    .AddStandardApiKey(inventory.ApiKey);

// OAuth2 client credentials
services.AddStandardHttpDependency("orders", orders)
    .AddStandardClientCredentials(orders.ClientCredentials);

// Your own: sign requests, add a tenant header, …
services.AddTransient<SigningHandler>();
services.AddStandardHttpDependency("partner", partner)
    .AddHttpMessageHandler<SigningHandler>();
```

**Client credentials** (RFC 6749 §4.4): the token is requested from `TokenUrl`, cached **per dependency** until shortly before it expires, and sent as `Authorization: Bearer`. The cache is a singleton, because HTTP handlers are recreated every few minutes and would otherwise lose it. A token endpoint that refuses the request, or answers without a token, throws `HttpRequestException`.

## Validation

`HttpDependencyOptionsValidator` runs at startup and names the key of every problem: absolute URLs, no local addresses in production, non-empty API key value and client credentials, a positive health check timeout.
