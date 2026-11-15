[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Request pipeline

Everything a request passes through before it reaches an endpoint, registered in three steps. **Authentication stays in the application, in the middle**, because the rate limiter must know the user.

```csharp
builder.AddStandardRequestPipeline(appSettings.RequestPipeline);   // registers everything

app.UseStandardRequestPipeline(appSettings.RequestPipeline);       // 1 forwarded headers, 2 request context, 3 exception handler,
                                                                   // 4 security headers, 5 compression, 6 CORS
app.UseAuthentication().UseAuthorization();                        // the application
app.UseStandardEndpointPolicies();                                 // 7 request user, 8 rate limiter, 9 timeouts, 10 output cache
```

Every option is documented in [request pipeline settings](./request-pipeline-settings.md). This page explains what each step does.

## 1. Forwarded headers

Behind a reverse proxy or load balancer the connection comes from the proxy, not from the client. `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host` carry the truth, but **only a trusted proxy may be believed**. List yours in `ForwardedHeaders:KnownProxies` (addresses) or `KnownNetworks` (CIDR, for example the Docker network `172.18.0.0/16`). Empty trusts only loopback, which is right for local development and wrong for production: the client address and scheme would be those of the proxy.

## 2. Request context

`RequestContext` is filled **once per request and cannot be overwritten**. Inject it anywhere. It carries:

| Value | Source |
|---|---|
| Correlation id | The `Correlation-Id` header, else `X-Request-ID`, else the trace id. A value must be 12 to 64 letters, digits, `.`, `_` or `-`; anything else is ignored. |
| Trace id, span id | The current activity. |
| Idempotency key | The `Idempotency-Key` header: 12 to 128 characters of the same set. **An invalid key is refused with 400**, not ignored. |
| Client address, user agent, host | The request (after forwarded headers). |
| User id | After authentication, by the request user step. |

The middleware also:

- **echoes the correlation id** on the response (`Correlation-Id`);
- **tags the span** (`aur.correlation.id`);
- **opens a log scope** with the correlation id and request fields, so every log line of the request carries them;
- passes the correlation id on to upstream calls made through [HTTP dependencies](./http-dependencies.md).

With [privacy](../core/privacy.md) on, personal values (client address, user agent, user id) are encrypted in the log scope and span; the context itself keeps the real ones.

This step runs **before** the exception handler, so even a failure carries the correlation id.

## 3. Errors

An unhandled exception becomes a `500` problem **without its message**, with the correlation id. Expected outcomes are returned as results, not thrown ([results and errors](../core/results-and-errors.md)).

| Situation | Answer | Logged as |
|---|---|---|
| Any unexpected exception | `500` problem, generic text | Error |
| A malformed request (bad JSON, bad parameter) | The status the framework chose (usually 400) | Warning |
| The caller disconnected before the response | `499` (the nginx convention) | Debug |
| An empty `401`, `403`, `404` or `405` | A problem body is added | |

The response never contains exception details.

## 4. Security headers

On every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`. In **production only**: HSTS and the `Content-Security-Policy` from `SecurityHeaders:ContentSecurityPolicy` (default `default-src 'none'; frame-ancestors 'none'`). Outside production they are left off so the API documentation UI can load.

## 5. Response compression

On by default, including over HTTPS (fine for bearer-token APIs; BREACH and CRIME mostly matter for cookie auth mixed with attacker-controlled input). Turn it off when a reverse proxy already compresses.

## 6. CORS

Optional. **Without policies nothing is registered** and browsers are limited to same-origin calls. Add policies only when a browser app calls the service directly from another origin. A policy named `default` (any casing) becomes the default policy; the others are named policies.

Startup refuses: an origin that is not `scheme://host[:port]` (no path, no trailing slash); `*` together with credentials; `*` in production; a local origin in production; two `default` policies.

## 7. Request user

After authentication: puts the user id into the request context, the log scope and the current span (encrypted when privacy is on; the span tag is protected again on export).

## 8. Rate limiting

**Named policies only; there is no global limiter.** An endpoint or a whole group opts in:

```csharp
group.RequireRateLimiting("standard");
```

Each caller gets its own **fixed window**: partitioned by the authenticated user id, or the client IP for anonymous requests. Over the limit the answer is `429 Too Many Requests` with a problem body. **Limits are per instance:** with N instances the effective limit is N times higher.

## 9. Timeouts

`Timeouts:DefaultSeconds` applies to every endpoint unless it names a policy or opts out:

```csharp
endpoint.WithRequestTimeout("long-running");
endpoint.DisableRequestTimeout();
```

A timeout only **cancels `HttpContext.RequestAborted`**. Your endpoint must pass that token on (declare a `CancellationToken` handler parameter and give it to EF Core, `HttpClient`, …) or the work continues. Timeouts are not enforced while a debugger is attached.

## 10. Output cache

**Nothing is cached unless an endpoint opts in** with a named policy:

```csharp
endpoint.CacheOutput("short");
```

The cache is **shared by all callers** unless the policy varies by a header: add `Authorization` to `VaryByHeader` when the response depends on who is asking.

## Kestrel limits

`KestrelLimits` sets the web server's limits (connections, header count, header timeout, keep-alive, body size). The defaults are Kestrel's own, listed so they are visible and easy to change.

## Idempotency

`.WithIdempotency()` on an endpoint makes it safe to retry. See [idempotency](./idempotency.md).
