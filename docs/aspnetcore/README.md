[Home](../../README.md) · [Documentation](../README.md)

# ASP.NET Core

`Aurore.Foundation.AspNetCore` is everything that makes a service an HTTP service: how callers authenticate, what a request passes through before reaching an endpoint, how endpoints are declared and versioned, how results become responses, how health is reported and how other services are called.

```
dotnet add package Aurore.Foundation.AspNetCore
```

It references [Core](../core/README.md), which holds the settings models this package reads.

## What is in it

| Area | What it gives you | Guide |
|---|---|---|
| Authentication | JWT bearer from one identity provider, API keys, provider-independent roles and scopes, endpoint requirements. | [Authentication](./authentication.md) |
| Request pipeline | Forwarded headers, request context and correlation id, exception handler, security headers, compression, CORS, rate limiting, timeouts, output cache, Kestrel limits. | [Request pipeline](./request-pipeline.md), [its settings](./request-pipeline-settings.md) |
| Idempotency | Safe retries of a request with an `Idempotency-Key`. | [Idempotency](./idempotency.md) |
| Minimal endpoints | `ResourceGroup` and `MinimalEndpoint<T>`: discovery, URL versions, deprecation and sunset, conventions that stack. | [Minimal endpoints](./minimal-endpoints.md) |
| Results to HTTP | `ToHttpResult()` and the problem body. | [Results to HTTP](./results-to-http.md) |
| Health checks | `/health/live`, `/health/ready`, `/health/status`. | [Health checks](./health-checks.md) |
| HTTP dependencies | Clients for the services this one calls: resilience, correlation id, API key, OAuth2 client credentials, health. | [HTTP dependencies](./http-dependencies.md) |
| Settings | Strict binding of `appsettings.json`, the validators, hosting. | [Settings](./settings.md) |

## The wiring order

The foundation's methods are named `AddStandard…`, `UseStandard…`, `MapStandard…`, `ConfigureStandard…`. Their order in `Program.cs` matters; [Getting started](../getting-started.md) shows the whole file. In short:

```
builder:  serializers → settings → hosting → telemetry → endpoints → request pipeline → (services) versioning, auth, health, validators, features, infra
app:      UseStandardRequestPipeline → UseAuthentication → UseAuthorization → UseStandardEndpointPolicies → map docs, health, resources
```

Authentication stays in the application, in the middle, because the rate limiter that comes after it partitions by user.
