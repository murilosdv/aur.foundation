# Aurore | Foundation

The building blocks of every Aurore service: errors and results, request pipeline, authentication, persistence, OpenAPI and telemetry. The [API archetype](https://github.com/murilosdv/aur.archetypes.api) shows them in use.

[Installing](./docs/installing.md) explains how to add a package.

New here? [Getting started](./docs/getting-started.md) wires a whole service and links each line to its guide.

## Packages

| Package | What it is | Docs | NuGet |
|---|---|---|---|
| `Aurore.Foundation.Core` | Results and errors, entities, features and decorators, validation, pagination, privacy, settings models, constants. No ASP.NET Core. | [Core](./docs/core/README.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.Core?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.Core) |
| `Aurore.Foundation.AspNetCore` | Authentication, request pipeline, idempotency, minimal endpoints and versioning, health checks, HTTP dependencies. | [ASP.NET Core](./docs/aspnetcore/README.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.AspNetCore?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.AspNetCore) |
| `Aurore.Foundation.OpenApi` | OpenAPI documents without a UI: one per version, security schemes, documented headers. | [OpenAPI](./docs/openapi/README.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.OpenApi?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.OpenApi) |
| `Aurore.Foundation.OpenApi.Scalar` | Scalar UI with sign-in by PKCE. | [Scalar](./docs/openapi/scalar.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.OpenApi.Scalar?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.OpenApi.Scalar) |
| `Aurore.Foundation.OpenApi.Swagger` | Swagger UI with sign-in by PKCE. | [Swagger](./docs/openapi/swagger.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.OpenApi.Swagger?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.OpenApi.Swagger) |
| `Aurore.Foundation.Telemetry` | OpenTelemetry logs, metrics and traces over OTLP, with privacy of personal tags. | [Telemetry](./docs/telemetry/README.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.Telemetry?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.Telemetry) |
| `Aurore.Foundation.EntityFrameworkCore` | EF Core without a database: conventions, soft delete, timestamps, async paging, unique-violation errors. | [EF Core](./docs/entity-framework/README.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.EntityFrameworkCore?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.EntityFrameworkCore) |
| `Aurore.Foundation.EntityFrameworkCore.Postgres` | PostgreSQL provider. | [Postgres](./docs/entity-framework/postgres.md) | [![NuGet](https://img.shields.io/nuget/vpre/Aurore.Foundation.EntityFrameworkCore.Postgres?label=nuget)](https://www.nuget.org/packages/Aurore.Foundation.EntityFrameworkCore.Postgres) |

## Conventions

- Wiring methods are `AddStandard…`, `UseStandard…`, `WithStandard…`. The application's own are `AddApp…`. Helpers used inside an endpoint or feature have no prefix.
- `Aurore` appears only in namespaces and package names. On the wire and in telemetry the mark is `aur.` / `AUR-`, held by [`PlatformNames`](./docs/core/constants.md#platformnames) and changeable.
- No default behavior hides in a package: decorators, interceptors and filters act only where the application opted in.
- Expected failures are returned as results, never thrown ([results and errors](./docs/core/results-and-errors.md)).
- Everything public has [XML documentation](./docs/instructions/xml-docs.md); a missing one fails the build.
- Tests cover our own logic only. See [Testing](./docs/instructions/testing.md).
- Files, pattern matching, braces and long declarations follow the [code style](./docs/instructions/code-style.md).

## Documentation

[Documentation index](./docs/README.md): guides (using each package) and instructions (working on them).

## Project

- [Changelog](./CHANGELOG.md)
- [Third-party notices](./NOTICE.md)
- [License](./LICENSE) ([MIT](https://mit-license.org))
