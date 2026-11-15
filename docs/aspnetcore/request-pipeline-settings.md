[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Request pipeline settings

Everything lives under `RequestPipeline` in `appsettings.json` and binds to `RequestPipelineOptions`. Every value has a default, so the section can be absent. What each step does is in [request pipeline](./request-pipeline.md).

```json
"RequestPipeline": {
  "ForwardedHeaders": { "KnownProxies": [], "KnownNetworks": [] },
  "KestrelLimits": { "MaxConcurrentConnections": null, "MaxRequestHeaderCount": 100, "RequestHeadersTimeoutSeconds": 30, "KeepAliveTimeoutSeconds": 130, "MaxRequestBodySizeBytes": 30000000 },
  "Idempotency": { "CacheDurationSeconds": 86400 },
  "CorsPolicies": {},
  "ResponseCompression": { "Enabled": true, "EnableForHttps": true },
  "SecurityHeaders": { "ContentSecurityPolicy": "default-src 'none'; frame-ancestors 'none'" },
  "RateLimiting": { "Policies": { "standard": { "PermitLimit": 100, "WindowSeconds": 60 } } },
  "Timeouts": { "DefaultSeconds": 100, "Policies": { "long-running": 300 } },
  "OutputCache": { "Policies": { "short": { "ExpirationSeconds": 10, "VaryByQuery": ["*"], "VaryByHeader": [] } } },
  "Privacy": { "Enabled": false, "ProtectedFields": [], "Keys": { "Current": "", "Previous": [] } }
}
```

## `ForwardedHeaders`

| Key | Default | Meaning |
|---|---|---|
| `KnownProxies` | `[]` | IP addresses of trusted reverse proxies or load balancers. Each must parse as an IP address. |
| `KnownNetworks` | `[]` | Trusted proxy networks in CIDR notation, for example `172.18.0.0/16`. |

Empty means only loopback is trusted. In production list your proxies, or `X-Forwarded-*` is ignored (wrong client IP and scheme).

## `KestrelLimits`

| Key | Default | Meaning |
|---|---|---|
| `MaxConcurrentConnections` | `null` (no limit) | The most connections open at once. Greater than 0 when set. |
| `MaxRequestHeaderCount` | 100 | The most headers one request may have. |
| `RequestHeadersTimeoutSeconds` | 30 | How long a client may take to send the headers. |
| `KeepAliveTimeoutSeconds` | 130 | How long an idle connection stays open. |
| `MaxRequestBodySizeBytes` | 30 000 000 | The largest request body, in bytes. `null` is no limit. Greater than 0 when set. |

## `Idempotency`

| Key | Default | Meaning |
|---|---|---|
| `CacheDurationSeconds` | 86 400 (24 h) | How long the first successful result is replayed for the same `Idempotency-Key`. Greater than 0. |

## `CorsPolicies`

A dictionary: policy name → policy. Empty (the default) means CORS is not enabled.

```json
"CorsPolicies": {
  "web": {
    "AllowedOrigins": ["https://app.example.com"],
    "AllowedMethods": ["GET", "POST"],
    "AllowedHeaders": [],
    "ExposedHeaders": [],
    "AllowCredentials": false,
    "PreflightMaxAgeSeconds": 600
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `AllowedOrigins` | `[]` | Origins (scheme, host, port) allowed to call the service. No path, no trailing slash. |
| `AllowedMethods` | `[]` | Methods the browser may use. Empty uses the platform's defaults. |
| `AllowedHeaders` | `[]` | Request headers the browser may send, merged with the foundation's: `Authorization`, `Content-Type`, `Correlation-Id`, `Idempotency-Key`, `traceparent`, `tracestate`. |
| `ExposedHeaders` | `[]` | Response headers the browser may read, merged with the foundation's: `Correlation-Id`, `traceparent`. |
| `AllowCredentials` | `false` | Only for cookie-based auth. Bearer tokens in `Authorization` do not need it. Cannot be combined with `*`. |
| `PreflightMaxAgeSeconds` | 0 | How long browsers may cache the preflight answer. Not negative. |

## `ResponseCompression`

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Turn off when a reverse proxy already compresses. |
| `EnableForHttps` | `true` | Compress over HTTPS too. |

## `SecurityHeaders`

| Key | Default | Meaning |
|---|---|---|
| `ContentSecurityPolicy` | `default-src 'none'; frame-ancestors 'none'` | Sent in **production only** (it would block the API documentation UI otherwise). `null` or empty omits it. |

## `RateLimiting`

| Key | Default | Meaning |
|---|---|---|
| `Policies` | `{}` | Name → policy. Endpoints opt in with `.RequireRateLimiting("name")`. |
| `Policies:<name>:PermitLimit` | 100 | Requests a caller may make in one window. Greater than 0. |
| `Policies:<name>:WindowSeconds` | 60 | The length of the window. Greater than 0. |

## `Timeouts`

| Key | Default | Meaning |
|---|---|---|
| `DefaultSeconds` | 100 | Applies to every endpoint unless it names a policy or disables timeouts. Greater than 0. |
| `Policies` | `{}` | Name → seconds (greater than 0). Used with `.WithRequestTimeout("name")`. |

## `OutputCache`

| Key | Default | Meaning |
|---|---|---|
| `Policies` | `{}` | Name → policy. Endpoints opt in with `.CacheOutput("name")`. |
| `Policies:<name>:ExpirationSeconds` | 60 | How long a cached response is served. Greater than 0. |
| `Policies:<name>:VaryByQuery` | `["*"]` | Query keys that produce separate entries. `*` is every key. |
| `Policies:<name>:VaryByHeader` | `[]` | Request headers that produce separate entries. Add `Authorization` when the response depends on the caller. |

## `Privacy`

See [privacy](../core/privacy.md) for the meaning and the key handling.

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Encrypt protected fields in logs and traces. |
| `ProtectedFields` | `[]` | Extra field names to protect. |
| `Keys:Current` | empty | 32 random bytes, base64. Required when enabled. |
| `Keys:Previous` | `[]` | Old keys, for reading old tokens. |

## Validation

`RequestPipelineOptionsValidator` runs at startup and names the key of every problem (for example `RequestPipeline:RateLimiting:Policies:standard:PermitLimit`). It checks that proxies are IP addresses and networks are valid CIDR, every number above is positive where stated, and the privacy keys are 32 base64 bytes when privacy is enabled. CORS rules (wildcard, local origins in production, origin format) are checked when the policies are registered.
