[Home](../../README.md) · [Documentation](../README.md) · [Entity Framework Core](./README.md)

# PostgreSQL

`Aurore.Foundation.EntityFrameworkCore.Postgres` is the PostgreSQL provider of [Entity Framework Core](./README.md). The base package describes entities, conventions, interceptors, paging and the unique-violation map; this package adds the **Npgsql registration, a snake case migrations history table, partial indexes, the unique-violation decorator and a health check**.

```
dotnet add package Aurore.Foundation.EntityFrameworkCore.Postgres
```

## Connection settings

`PostgresOptions` is a free-form map of **the keywords Npgsql knows**, so nothing is hidden behind a fixed shape (pooling, SSL, timeouts, … all work):

```json
"Postgres": {
  "Host": "db.example.com",
  "Port": "5432",
  "Database": "app",
  "Username": "app",
  "Password": "<secret>",
  "SSL Mode": "VerifyFull"
}
```

- Keys are **case-insensitive**; an unknown keyword is rejected when the connection string is built.
- `PostgresOptionsValidator(isProduction)` requires `Host`, `Database`, `Username` and `Password`, and in **production rejects a local host**.
- **Supply the password through the environment** (`Postgres__Password`), not a file.
- `options.ToConnectionString()` returns the Npgsql connection string, for the migrations tool or a health check.

## Registering the context

```csharp
services.AddSingleton(TimeProvider.System);
services.AddStandardDbContext<AppDbContext>(appSettings.Postgres);
```

Registers Npgsql with the connection from the options, no-tracking queries and the base interceptors. An optional third argument adjusts the `DbContextOptionsBuilder` further (`builder => builder.EnableSensitiveDataLogging()`).

The migrations history table keeps its Npgsql name (`__EFMigrationsHistory`) but gets snake case columns and key. It is built apart from `OnModelCreating`, so the naming convention would not reach it; the package does it (`UseSnakeCaseNpgsqlHistoryTable`).

## Columns and indexes

| Helper | Effect |
|---|---|
| `index.WhereNotDeleted()` | Makes the index **partial**: `WHERE "deleted_at" IS NULL`, so a deleted row's value can be reused. |
| `property.AsTimestampColumn(hasTimezone = true)` | Stores a date-time as `timestamptz` (default) or `timestamp` (`hasTimezone: false`). |

Searching case-insensitively with `ILike` needs the escape character stated: `EF.Functions.ILike(column, pattern, "\\")`.

`DatabaseDefaults.Schema` is `app`, the same as the base package.

## Unique violations

`WithUniqueViolation()` (or `[WithUniqueViolation]`) enables the decorator for SQLSTATE **`23505`** (`PostgresSqlStates.UniqueViolation`). The answer is the error registered for the index in `AddUniqueViolationErrors`, or `database.conflict` (409) with the violated fields as details read from the model. The constraint name is never sent. See [unique violations](./unique-violations.md), including that **Postgres aborts the transaction** on a violation.

## Health check

```csharp
services.AddPostgresHealthCheck(appSettings.Postgres);
```

Adds a check named `postgres` that opens a connection and runs `SELECT 1`, tagged `readiness`, `type-database` and `critical`: when the database is down the service reports Unhealthy on `/health/ready`. See [health checks](../aspnetcore/health-checks.md).

## Telemetry

The telemetry package listens to Npgsql's `ActivitySource` and meter, so every command is a span and the pool and command metrics are exported ([telemetry](../telemetry/README.md)).

## Tests

The package's tests run on a real PostgreSQL server ([testing](../instructions/testing.md)).
