[Home](./README.md)

# Changelog

All notable changes to this project will be documented in this file.

Read [the instructions file](./docs/instructions/changelog.md) to learn what and how to write here.

## [OpenApi.Scalar-1.0.0-preview.4] - 2026-11-23

### Changed

- `Scalar.AspNetCore.Microsoft` updated from 3.1.1 to 3.2.0.

---

## [AspNetCore-1.0.0-preview.4] - 2026-11-22

### Changed

- Internal: the project file is organized in a fixed order (properties, framework reference, internals visible to, packages, project references, files, usings). No change in behavior or public API.

---

## [Core-1.0.0-preview.4] - 2026-11-22

### Changed

- Internal: the project file is organized in a fixed order (properties, framework reference, internals visible to, packages, project references, files, usings). No change in behavior or public API.

---

## [Telemetry-1.0.0-preview.4] - 2026-11-22

### Changed

- Internal: the project file is organized in a fixed order (properties, framework reference, internals visible to, packages, project references, files, usings). No change in behavior or public API.

---

## [EntityFrameworkCore-1.0.0-preview.4] - 2026-11-22

### Changed

- Internal: the project file is organized in a fixed order (properties, framework reference, internals visible to, packages, project references, files, usings). No change in behavior or public API.

---

## [EntityFrameworkCore.Postgres-1.0.0-preview.4] - 2026-11-22

### Changed

- Internal: the project file is organized in a fixed order (properties, framework reference, internals visible to, packages, project references, files, usings). No change in behavior or public API.

---

## [AspNetCore-1.0.0-preview.3] - 2026-11-22

### Added

- `RequireJwtBearerAuthentication()` and `RequireApiKeyAuthentication()` select one registered authentication scheme on an endpoint or a resource group, and compose with the role and scope helpers.

### Fixed

- A replayed idempotent response stops when the client has already gone away, instead of writing to a closed connection.
- Two endpoints with the same final name, set with `WithName` or the `Name` property, stop the application from starting. A `WithName` duplicate used to start fine and answer 500 on the first request, and a repeated `Name` property was rejected even when `WithName` made the names unique.
- Endpoints named by default or by the `Name` property now also carry the route name metadata, so link generation finds them.

### Changed

- Internal: a few loops written with LINQ, an unnecessary array and a null-forgiving operator removed, and the sort field is an interface. No change in behavior.
- The parameter of `UnexpectedExceptionHandler.TryHandleAsync` is named `httpContext`, as in `IExceptionHandler`.
- The default API version is no longer set explicitly: 1.0 is already the default. No change in behavior.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [Core-1.0.0-preview.3] - 2026-11-22

### Changed

- Internal: a few loops written with LINQ, an unnecessary array and a null-forgiving operator removed, and the sort field is an interface. No change in behavior.
- The validator interfaces `IRequestValidator<T>` and `IFeatureValidator<T>` are contravariant (`in T`): a validator of a base type can be used where a validator of a derived type is expected. Registrations and resolution are not affected.
- Internal: the validator of the request pipeline settings has one method per section, instead of one long constructor. Same checks, same messages.
- Internal: the cancellation token is the last parameter of the feature pipeline entry, `Keyset` and the feature registration return their narrower types, and the dictionaries with a comparer use collection expressions. No change in behavior or public API.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [OpenApi-1.0.0-preview.3] - 2026-11-22

### Changed

- Internal: a few loops written with LINQ, an unnecessary array and a null-forgiving operator removed, and the sort field is an interface. No change in behavior.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [OpenApi.Scalar-1.0.0-preview.3] - 2026-11-22

### Changed

- Internal: a few loops written with LINQ, an unnecessary array and a null-forgiving operator removed, and the sort field is an interface. No change in behavior.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [OpenApi.Swagger-1.0.0-preview.3] - 2026-11-22

### Changed

- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [Telemetry-1.0.0-preview.3] - 2026-11-22

### Changed

- Internal: the startup line is written by a source-generated logger message, so its arguments are not evaluated when the level is off. No change in behavior.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [EntityFrameworkCore-1.0.0-preview.3] - 2026-11-22

### Changed

- Internal: a few loops written with LINQ, an unnecessary array and a null-forgiving operator removed, and the sort field is an interface. No change in behavior.
- Internal: the unique violation answer shares one message array instead of creating one per field, and the map uses a collection expression. No change in behavior or public API.
- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [EntityFrameworkCore.Postgres-1.0.0-preview.3] - 2026-11-22

### Changed

- Package metadata: a README of its own on nuget.org (with absolute links), a release notes link, the copyright line as in the license, and the symbol package pushed with it.
- NuGet versions of the dependencies moved to `Directory.Packages.props` (central package management); same versions, same package dependencies.

---

## [AspNetCore-1.0.0-preview.2] - 2026-11-18

### Fixed

- The route name, summary and description of a minimal endpoint now follow one order, the last one wins: the derived default, then the `Name`, `Summary` and `Description` properties, then `WithName`, `WithSummary` and `WithDescription` inside `Map`. Before, the property or the derived value always overwrote the calls made in `Map`.
- Two endpoints with the same name, set with `WithName` or the `Name` property, stop the application from starting. A `WithName` duplicate used to start fine and answer 500 on the first request.

### Changed

- Internal cleanup, no change in behavior or public API: conditions written as pattern matching, one-line blocks without braces and long signatures wrapped.

---

## [Core-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: conditions written as pattern matching, long signatures wrapped, the API key option validation written as a `switch` (same checks, same messages), and `SoftDeletableEntity` and `WithAttribute<T>` in files of their own, in the same namespaces.

---

## [OpenApi-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: conditions written as pattern matching and long signatures wrapped.

---

## [OpenApi.Scalar-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: long signatures wrapped.

---

## [OpenApi.Swagger-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: long signatures wrapped.

---

## [Telemetry-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: conditions written as pattern matching, long signatures wrapped and an empty block removed.

---

## [EntityFrameworkCore-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: conditions written as pattern matching and long signatures wrapped.

---

## [EntityFrameworkCore.Postgres-1.0.0-preview.2] - 2026-11-18

### Changed

- Internal cleanup, no change in behavior or public API: long signatures wrapped and `WithUniqueViolationAttribute` in a file of its own, in the same namespace.

---

## [Core-1.0.0-preview.1] - 2026-11-15

### Added

- Results and errors that map to HTTP, entities with timestamps and soft delete, features with opt-in decorators (telemetry, logging, validation), numbered and cursor pagination with a whitelist of sortable fields, and encryption of personal data in logs and traces. `FeatureOrder` names the places of the decorators, and applications can inherit it. `FieldLengths` and `Patterns` are shared constants for validators and columns. `PlatformNames` holds the names added to telemetry and requests and can change their prefix.

---

## [AspNetCore-1.0.0-preview.1] - 2026-11-15

### Added

- JWT bearer and API key authentication with provider-independent roles and scopes, the request pipeline (correlation id, idempotency, security headers, rate limiting), minimal endpoints with versions, health checks and HTTP dependencies with resilience and OAuth2 client credentials.

---

## [OpenApi-1.0.0-preview.1] - 2026-11-15

### Added

- OpenAPI documents without a UI: one per API version, document info, security schemes (bearer, API key, OpenID Connect, OAuth2) decided per endpoint, camel case query parameters and the documented correlation id and idempotency key headers.

---

## [OpenApi.Scalar-1.0.0-preview.1] - 2026-11-15

### Added

- The Scalar reference UI for those documents, with one entry per version and sign-in with PKCE.

---

## [OpenApi.Swagger-1.0.0-preview.1] - 2026-11-15

### Added

- Swagger UI for those documents, with one entry per version and sign-in with PKCE.

---

## [Telemetry-1.0.0-preview.1] - 2026-11-15

### Added

- OpenTelemetry logs, metrics and traces over OTLP.

---

## [EntityFrameworkCore-1.0.0-preview.1] - 2026-11-15

### Added

- Entity Framework Core support that knows no database: conventions (snake case, schema, soft delete filter), timestamp and soft delete interceptors, async paging and the errors and map of unique violations.

---

## [EntityFrameworkCore.Postgres-1.0.0-preview.1] - 2026-11-15

### Added

- The PostgreSQL provider: Npgsql registration, snake case history table, partial indexes, a decorator that answers unique violations with a conflict and a health check.

---
