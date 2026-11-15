[Home](../../README.md) · [Documentation](../README.md)

# Telemetry

`Aurore.Foundation.Telemetry` sends **logs, metrics and traces over OTLP to one OpenTelemetry Collector**, which decides where the data goes next. The service knows one address; backends can change without touching the service.

```
dotnet add package Aurore.Foundation.Telemetry
```

```csharp
builder.AddStandardTelemetry(appSettings.AppInformation, appSettings.Telemetry);
```

It also registers a console logger so logs stay visible when the collector is unreachable: **single-line, with a time of day, in Development; JSON with UTC ISO-8601 timestamps and scopes elsewhere** (one JSON object per line, ready for a container log driver). The default logging providers are cleared first.

At startup the effective protocol, endpoint and sampling ratio are logged once, because OTLP export failures are silent (the SDK only reports them through its own diagnostics), so a wrong endpoint or protocol would otherwise go unnoticed.

## Settings

```json
"Telemetry": {
  "Otlp": { "Endpoint": "http://collector:4317", "Protocol": "Grpc", "Headers": "", "RequestTimeoutSeconds": 10 },
  "ResourceAttributes": { "deployment.region": "eu-west-1" },
  "Logging": { "IncludeScopes": true, "ParseStateValues": true, "BatchExport": { } },
  "Metrics": { "ExportToConsole": false, "ExportIntervalSeconds": 60, "ExportTimeoutSeconds": 30 },
  "Tracing": { "SamplingRatio": 1.0, "ExcludedIncomingPaths": [], "ExcludedOutgoingPaths": [], "ExportToConsole": false, "BatchExport": { } }
}
```

### `Otlp`: the collector

| Key | Default | Meaning |
|---|---|---|
| `Endpoint` | required | Base URL of the collector: `http://collector:4317` for gRPC or `http://collector:4318` for HTTP. Absolute; **a local address is refused in production**. |
| `Protocol` | required | `Grpc` or `HttpProtobuf` (any casing). |
| `Headers` | empty | Extra headers sent with every export, as comma-separated `key=value` pairs (for example an authentication token). **Secrets belong in environment variables or a secret store.** |
| `RequestTimeoutSeconds` | 10 | Timeout of each request to the collector. Greater than 0. |

- **gRPC** (port 4317) uses the endpoint as is.
- **HTTP/protobuf** (port 4318) needs the per-signal path (`/v1/traces`, `/v1/metrics`, `/v1/logs`). The SDK does not add it when the endpoint is set in code, so the package appends it: configure only the base URL.

### `ResourceAttributes`

Extra attributes attached to **everything** the service exports, such as the region. Name → value.

### `Logging`

| Key | Default | Meaning |
|---|---|---|
| `IncludeScopes` | `true` | Export log scopes (correlation id, user id, …) with each log record. |
| `ParseStateValues` | `true` | Export the structured values of the message as attributes. |
| `BatchExport` | see below | How log records are batched. |

### `Metrics`

| Key | Default | Meaning |
|---|---|---|
| `ExportToConsole` | `false` | Also write metrics to the console, useful without a collector. |
| `ExportIntervalSeconds` | 60 | Interval between two exports. Greater than 0. |
| `ExportTimeoutSeconds` | 30 | Maximum time an export may take. Greater than 0. |

### `Tracing`

| Key | Default | Meaning |
|---|---|---|
| `SamplingRatio` | 1.0 | Fraction of traces kept, 0 to 1: `1.0` keeps all, `0.1` keeps one in ten. **Head sampling**, decided when the trace starts and parent-based (a request keeps the decision of its caller). To keep errors and slow traces, leave it at 1.0 and sample in the Collector. |
| `ExcludedIncomingPaths` | `[]` | Path prefixes of incoming requests that are not traced. |
| `ExcludedOutgoingPaths` | `[]` | Path prefixes of outgoing HTTP calls that are not traced. |
| `ExportToConsole` | `false` | Also write spans to the console. |
| `BatchExport` | see below | How spans are batched. |

`/health/` is **never traced**, whatever is configured: probes are frequent and uninteresting.

### `BatchExport` (logs and traces)

| Key | Default | Meaning |
|---|---|---|
| `MaxQueueSize` | 2048 | Records buffered in memory; when full, new records are **dropped**. |
| `MaxExportBatchSize` | 512 | Most records sent per export. Not above `MaxQueueSize`. |
| `ScheduledDelaySeconds` | 5 | Delay between two exports. |
| `ExportTimeoutSeconds` | 30 | Maximum time an export may take before being cancelled. |

The defaults are the OpenTelemetry SDK's. All values must be greater than 0.

## What is collected

- **Resource** (on everything): `service.name` (the component, slug case), `service.namespace` (`domain.boundary`, slug case), `service.version` (the assembly version), `service.instance.id` (the machine name), `deployment.environment.name`, plus the platform attributes `aur.service.domain`, `aur.service.boundary`, `aur.service.maintainer` ([`PlatformNames`](../core/constants.md#platformnames)) and your `ResourceAttributes`.
- **Traces**: ASP.NET Core requests, outgoing HTTP calls, Npgsql commands, and the application's own spans: the source named by `AppInformation.ActivitySource`, which the [feature telemetry decorator](../core/features.md#telemetry) and `AppDiagnostics.StartActivity` use.
- **Metrics**: ASP.NET Core, HTTP client, .NET runtime, process and Npgsql.
- **Logs**: everything the host logs, with scopes.
- **Request fields** the pipeline puts on spans and in the log scope: `aur.correlation.id`, the user id, the client address and user agent. With [privacy](../core/privacy.md) on, the personal ones are encrypted: a trace processor re-protects the span tags when the span ends, before any exporter sees them.

## Custom metrics and trace sources

The package registers its own meters and sources. Yours must be registered too, or they are ignored. Use the OpenTelemetry hosting API on the services, which **adds** to what the package configured:

```csharp
public static class AppMetrics
{
    public static readonly Meter Meter = new("Acme.Billing");
    public static readonly Counter<long> InvoicesIssued = Meter.CreateCounter<long>("invoices.issued");
}

builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter("Acme.Billing"));
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource("Acme.Billing.Checkout"));

AppMetrics.InvoicesIssued.Add(1, new KeyValuePair<string, object?>("channel", "web"));

using var activity = checkoutSource.StartActivity("ProcessInvoice");   // null when nobody listens
activity?.SetTag("invoice.id", id);
```

Spans on the application's main source need no registration: use `AppDiagnostics.StartActivity("name")` ([application](../core/application.md#appdiagnostics)).

## Validation

`TelemetryOptionsValidator(isProduction)` checks the collector (absolute URL, no local address in production, a known protocol, positive timeout), the batch settings, `SamplingRatio` in 0..1 and the metric intervals.

## Troubleshooting

- **Nothing arrives:** read the startup line; check the protocol matches the port (gRPC 4317, HTTP 4318). Turn on `ExportToConsole` to see the signals without a collector.
- **Spans from my code are missing:** the source is not registered (see above), or `SamplingRatio` dropped the trace.
- **Logs lose records under load:** `MaxQueueSize` is full; raise it or shorten `ScheduledDelaySeconds`.
