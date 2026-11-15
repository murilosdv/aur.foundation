[Home](./README.md)

# Changelog

All notable changes to this project will be documented in this file.

Read [the instructions file](./docs/instructions/changelog.md) to learn what and how to write here.

## [Unreleased]

### Added

- `Aurore.Foundation.Core`: results and errors that map to HTTP, entities with timestamps and soft delete, features with opt-in decorators (telemetry, logging, validation), numbered and cursor pagination with a whitelist of sortable fields, and encryption of personal data in logs and traces. `FeatureOrder` names the places of the decorators, and applications can inherit it. `FieldLengths` and `Patterns` are shared constants for validators and columns. `PlatformNames` holds the names added to telemetry and requests and can change their prefix.
- `Aurore.Foundation.AspNetCore`: JWT bearer and API key authentication with provider-independent roles and scopes, the request pipeline (correlation id, idempotency, security headers, rate limiting), minimal endpoints with versions, health checks and HTTP dependencies with resilience and OAuth2 client credentials.
- `Aurore.Foundation.OpenApi`: OpenAPI documents without a UI: one per API version, document info, security schemes (bearer, API key, OpenID Connect, OAuth2) decided per endpoint, camel case query parameters and the documented correlation id and idempotency key headers.
- `Aurore.Foundation.OpenApi.Scalar`: the Scalar reference UI for those documents, with one entry per version and sign-in with PKCE.
- `Aurore.Foundation.OpenApi.Swagger`: Swagger UI for those documents, with one entry per version and sign-in with PKCE.
- `Aurore.Foundation.Telemetry`: OpenTelemetry logs, metrics and traces over OTLP.
- `Aurore.Foundation.EntityFrameworkCore`: Entity Framework Core support that knows no database: conventions (snake case, schema, soft delete filter), timestamp and soft delete interceptors, async paging and the errors and map of unique violations.
- `Aurore.Foundation.EntityFrameworkCore.Postgres`: the PostgreSQL provider: Npgsql registration, snake case history table, partial indexes, a decorator that answers unique violations with a conflict and a health check.
- `Aurore.Foundation.EntityFrameworkCore.SqlServer`: the SQL Server provider: clustering on a sequential column for Guid keys (`UseSequentialClusteredIndex`), SqlClient registration, filtered indexes, column helpers, a decorator that answers duplicate key errors with a conflict (needs the server messages in English) and a health check.
- Delivery: each package has its own version in `Versions.props`, published as written. Pull requests check Conventional Commits, formatting, tests and coverage for the changed packages only, plus CodeQL; a merge to `main` publishes them to GitHub Packages and creates one release from these notes. A manual job cleans old package versions. The `start-services` action lets other repositories start the Postgres or SQL Server their tests need.
- Tests run in a container (`make test`) with a coverage report, next to a local run (`make test-local`); the EF tests are written once and run against real PostgreSQL and SQL Server.
- Documentation: a getting-started page, a folder of guides per package (every setting, what it means and its default, with examples), and instructions for testing, publishing, shared actions, XML documentation and this changelog. Every public member is documented, and a missing summary fails the build.

---
