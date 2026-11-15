[Home](../../README.md) · [Documentation](../README.md)

# Core

`Aurore.Foundation.Core` is the part of the foundation that has no ASP.NET Core in it. It is what a service's business code, a worker or a test project can reference alone: the vocabulary of results and errors, entities, features and their decorators, validation, pagination, privacy and the settings models that every other package reads.

```
dotnet add package Aurore.Foundation.Core
```

## What is in it

| Area | What it gives you | Guide |
|---|---|---|
| Results and errors | `Result`, `Result<T>`, `ApplicationError`, `ErrorKind`, `StandardErrors`: expected failures as values, not exceptions. | [Results and errors](./results-and-errors.md) |
| Features | `Feature<TRequest>` and `Feature<TRequest, TResponse>`: one class per use case, wrapped by opt-in decorators (telemetry, logging, validation). | [Features](./features.md) |
| Validation | `RequestValidator<T>` for the shape of a request, `FeatureValidator<T>` for business rules, and the conversion of FluentValidation failures to errors. | [Validation](./validation.md) |
| Pagination | Numbered pages, cursor pages, the `SortMap` whitelist of sortable fields, timestamp ranges. | [Pagination](./pagination.md) |
| Entities | `Entity`, `SoftDeletableEntity` and the interfaces behind timestamps and soft delete. | [Entities](./entities.md) |
| Privacy | `IPrivacyProtector`: AES-256-GCM encryption of personal data on its way to logs and traces. | [Privacy](./privacy.md) |
| Application settings | `AppInformationOptions`, `HostingOptions`, `ApiDocsOptions`, `AppVersion`, `AppDiagnostics`, `ExitCodes`. | [Application](./application.md) |
| Settings models | The options records of the other packages live here (identity provider, API keys, request pipeline, HTTP dependencies, health checks, pagination), with their validators. | each package's guide |
| Constants | `FieldLengths`, `Patterns`, `PlatformNames`, `OpenTelemetryProperties`, `TimestampFormats`. | [Constants](./constants.md) |
| JSON | The one set of JSON rules and the helpers to use them. | [Serialization](./serialization.md) |
| Extensions | String, URL, enum, path and OS helpers. | [Extensions](./extensions.md) |

## How the pieces fit

A request that reaches a service goes through this chain. Core owns the parts in bold:

```
HTTP endpoint ── validates the shape ──► **RequestValidator**
      │
      ▼
**Feature** ── decorators: **telemetry → logging → validation (FeatureValidator)** ── logic ── repository / EF Core
      │
      ▼
**Result<T>** (value or **ApplicationError**) ── ToHttpResult() ──► 200 / 204 / problem+json
```

The endpoint layer (ASP.NET Core package) never decides what a failure means: it reads `ApplicationError.Kind` and maps it to a status. The feature never knows it is called over HTTP, so the same feature runs in a worker or a test unchanged.

## Conventions this package follows

- **Failures that can happen are returned**, never thrown. Exceptions are for bugs and outages and become a 500.
- **Nothing is on unless asked for.** Decorators, filters and interceptors act only where the application opted in.
- **Settings are plain records** that bind from `appsettings.json`, with a validator that names the configuration key of every problem and is stricter in production.
- **Everything public has XML documentation**, so the editor shows what each option means.
