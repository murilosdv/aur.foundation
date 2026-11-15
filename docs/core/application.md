[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Application settings and startup helpers

The settings every service has, and the small types that run at startup.

## `AppInformation`: who this service is

```json
"AppInformation": {
  "Domain": "Acme",
  "Boundary": "Billing",
  "Component": "InvoiceApi",
  "Description": "Issues and tracks invoices.",
  "Maintainer": { "Name": "billing-team", "Email": "billing@example.com", "Url": "https://example.com/teams/billing" },
  "Locale": "en-US"
}
```

| Key | Required | Meaning |
|---|---|---|
| `Domain` | yes | The business domain the service belongs to; the first part of the namespace. |
| `Boundary` | yes | The bounded context inside the domain; the second part. |
| `Component` | yes | The name of this service inside the boundary. |
| `Description` | yes | A short description of what the service does. Shown in the OpenAPI document. |
| `Maintainer` | yes | `Name`, `Email` and optional `Url` of the team to contact. Shown in the OpenAPI document and added to telemetry. |
| `Locale` | no (`en-US`) | The default locale of the service. |

Read-only values derived from them:

| Property | Value |
|---|---|
| `Version` | Not configurable: read from the entry assembly (see `AppVersion`) so it cannot drift from what is deployed. |
| `Namespace` | `Domain.Boundary`, for example `Acme.Billing`. |
| `ActivitySource` | `Namespace.Component`, for example `Acme.Billing.InvoiceApi`: the name of the shared `ActivitySource`. |

These names are used for the telemetry resource (`service.name`, `aur.service.domain`, …), the activity source, the health liveness answer and the OpenAPI title.

## `Hosting`

```json
"Hosting": { "ShutdownTimeoutSeconds": 10 }
```

How long the host waits for in-flight requests to finish after a stop signal before giving up. The default (10) matches `docker stop`. **If you raise it, raise `stop_grace_period` in compose to at least this value plus a few seconds**, or Docker kills the process first. Applied by `builder.ConfigureStandardHosting(appSettings.Hosting)`.

## `ApiDocs`

```json
"ApiDocs": { "OAuthClientId": "api-docs" }
```

The client id of a **public** identity-provider client used to sign in from the interactive API documentation with the authorization code flow and PKCE. A public client has no secret, so the id is not sensitive. Empty leaves the sign-in unconfigured (a token can still be pasted by hand). The documentation only runs outside production. See [Scalar](../openapi/scalar.md) and [Swagger](../openapi/swagger.md).

## `AppVersion`

`AppVersion.Current` is the informational version of the entry assembly: `<Version>` in the project file, or `dotnet publish -p:Version=1.4.2` in CI. The SDK appends `+<commit sha>`; that suffix is dropped. `0.0.0` when it cannot be read.

## `AppDiagnostics`

The application's shared `ActivitySource`, so every part of the app creates its spans under one source name.

```csharp
AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);   // once, at startup

using var activity = AppDiagnostics.StartActivity("ProcessOrder", [new("order.id", id)]);   // null when nobody listens
```

| Member | Does |
|---|---|
| `Initialize(name)` | Creates the source. Calling it again with the same name is harmless; a different name throws. |
| `Source` | The source. Throws when `Initialize` has not been called. |
| `StartActivity(name, tags?)` | Starts an internal span. Returns `null` before `Initialize` or when nothing listens. |

The [telemetry package](../telemetry/README.md) listens to this source, and the [telemetry decorator](./features.md#telemetry) uses it.

## `ExitCodes`

Process exit codes following `sysexits.h`, for `Program.cs`:

| Constant | Value | When |
|---|---|---|
| `Success` | 0 | Finished normally. |
| `RuntimeError` | 70 | The application started and then failed. |
| `ConfigurationError` | 78 | The application could not start because its configuration is invalid. |

An orchestrator (and a human reading `docker ps`) can tell a crash from a bad configuration by the code. See [getting started](../getting-started.md) for the `Program.cs` that uses them.
