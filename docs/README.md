[Home](../README.md)

# Documentation

Two kinds of page: **guides** explain how to use a package, setting by setting; **instructions** explain how to work on the foundation itself. New to it? Read [Getting started](./getting-started.md) first: it wires a whole service and links each line to its guide.

To restore the packages, add the registry first: [Installing](./installing.md).

## Guides, by package

### [Core](./core/README.md): no ASP.NET Core

| Guide | About |
|---|---|
| [Results and errors](./core/results-and-errors.md) | `Result`, `ApplicationError`, kinds, `StandardErrors`: expected failures as values. |
| [Features and decorators](./core/features.md) | One class per use case; telemetry, logging and validation decorators; writing your own; order. |
| [Validation](./core/validation.md) | `RequestValidator` (shape) and `FeatureValidator` (business rules); field errors. |
| [Pagination](./core/pagination.md) | Numbered and cursor pages, `SortMap`, timestamp ranges, errors, settings. |
| [Entities](./core/entities.md) | `Entity`, `SoftDeletableEntity`, timestamps, soft delete, the version 7 id. |
| [Privacy](./core/privacy.md) | Encrypting personal data in logs and traces; keys and rotation. |
| [Application](./core/application.md) | `AppInformation`, `Hosting`, `ApiDocs`, `AppVersion`, `AppDiagnostics`, `ExitCodes`. |
| [Constants](./core/constants.md) | `FieldLengths`, `Patterns`, `PlatformNames` and the prefix. |
| [JSON serialization](./core/serialization.md) | The single set of JSON rules and the helpers. |
| [Extensions](./core/extensions.md) | String, URL, enum, path and OS helpers. |

### [ASP.NET Core](./aspnetcore/README.md)

| Guide | About |
|---|---|
| [Authentication and authorization](./aspnetcore/authentication.md) | JWT bearer, API keys, roles and scopes, every setting. |
| [Request pipeline](./aspnetcore/request-pipeline.md) | The ten steps a request passes through. |
| [Request pipeline settings](./aspnetcore/request-pipeline-settings.md) | Every key under `RequestPipeline`. |
| [Idempotency](./aspnetcore/idempotency.md) | Safe retries with `Idempotency-Key`. |
| [Minimal endpoints](./aspnetcore/minimal-endpoints.md) | Resource groups, endpoints, versions, conventions, startup checks. |
| [Results to HTTP](./aspnetcore/results-to-http.md) | `ToHttpResult()`, status mapping, the problem body. |
| [Health checks](./aspnetcore/health-checks.md) | Live, ready and status endpoints; tags; critical dependencies. |
| [HTTP dependencies](./aspnetcore/http-dependencies.md) | Clients for called services: resilience, correlation id, API key, client credentials, health. |
| [Settings](./aspnetcore/settings.md) | Strict binding, validators, environments, secrets. |

### [OpenAPI](./openapi/README.md)

| Guide | About |
|---|---|
| [OpenAPI documents](./openapi/README.md) | One document per version, security schemes, headers. |
| [Scalar](./openapi/scalar.md) | Scalar UI with sign-in by PKCE. |
| [Swagger UI](./openapi/swagger.md) | Swagger UI with sign-in by PKCE. |

### [Telemetry](./telemetry/README.md)

Logs, metrics and traces over OTLP: every setting, what is collected, custom metrics and sources, troubleshooting.

### [Entity Framework Core](./entity-framework/README.md)

| Guide | About |
|---|---|
| [Overview](./entity-framework/README.md) | Registering the context, entities, the model, column helpers. |
| [Conventions and interceptors](./entity-framework/conventions-and-interceptors.md) | Snake case, schema, soft delete filter, timestamps, what is not covered. |
| [Unique violations](./entity-framework/unique-violations.md) | Answering duplicates with a conflict; the constraint map. |
| [Migrations](./entity-framework/migrations.md) | The tools project and the design-time factory. |
| [PostgreSQL](./entity-framework/postgres.md) | Connection settings, partial indexes, health check. |

## Instructions

- [Testing](./instructions/testing.md)
- [Publishing](./instructions/publishing.md): versions and the pipelines.
- [Shared actions](./instructions/shared-actions.md): actions other repositories can reuse.
- [Changelog](./instructions/changelog.md)
- [XML documentation](./instructions/xml-docs.md)
- [Code style](./instructions/code-style.md): files, pattern matching, braces, long declarations.
