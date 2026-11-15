[Home](../../README.md) · [Documentation](../README.md) · [Entity Framework Core](./README.md)

# SQL Server

`Aurore.Foundation.EntityFrameworkCore.SqlServer` is the SQL Server provider of [Entity Framework Core](./README.md). The base package describes entities, conventions, interceptors, paging and the unique-violation map; this package adds the **SqlClient registration, filtered indexes, column helpers, clustered Guid keys, the unique-violation decorator and a health check**.

```
dotnet add package Aurore.Foundation.EntityFrameworkCore.SqlServer
```

## Connection settings

`SqlServerOptions` is a free-form map of **the keywords `Microsoft.Data.SqlClient` knows**:

```json
"SqlServer": {
  "Data Source": "db.example.com,1433",
  "Initial Catalog": "app",
  "User ID": "app",
  "Password": "<secret>",
  "Encrypt": "True"
}
```

- Keys are **case-insensitive**; an unknown keyword is rejected.
- `SqlServerOptionsValidator(isProduction)` requires the data source and the database (`Initial Catalog`), and in **production requires encryption and rejects `TrustServerCertificate`** (a validated server certificate).
- It does **not** require a password, so integrated security and Microsoft Entra identities work.
- **Supply secrets through the environment** (`SqlServer__Password`), not a file.
- `options.ToConnectionString()` returns the connection string.

## Registering the context

```csharp
services.AddSingleton(TimeProvider.System);
services.AddStandardDbContext<AppDbContext>(appSettings.SqlServer);
```

Registers SQL Server with the connection from the options, no-tracking queries and the base interceptors, and puts the migrations history table in the `app` schema as `ef_migrations_history`. An optional third argument adjusts the `DbContextOptionsBuilder`.

## Columns and indexes

| Helper | Effect |
|---|---|
| `index.WhereNotDeleted()` | Makes the index **filtered**: `[deleted_at] IS NULL`, so a deleted row's value can be reused. |
| `property.AsTimestampColumn(hasTimezone = true)` | A `DateTimeOffset` becomes `datetimeoffset` (default); a `DateTime` becomes `datetime2` (`hasTimezone: false`). The other combination throws. SQL Server's `timestamp` is a row version, not a date, so it is not used. |
| `text.EscapeLikePattern()` | Escapes `[`, `%` and `_` so `EF.Functions.Like` matches the text literally. |

## Guid keys and the clustered index

SQL Server sorts a `Guid` by its **last bytes**, so even a version 7 Guid arrives out of order in a clustered index and the table fragments as it grows.

```csharp
protected override void Extend(EntityTypeBuilder<Post> builder)
{
    builder.UseSequentialClusteredIndex();
}
```

`UseSequentialClusteredIndex()` keeps the Guid as a **non-clustered primary key** and clusters the table on a shadow `bigint` identity column (`SequentialId`, not on the entity class) with the unique index `CX_<table>`.

**The cost:** a lookup by Guid takes one more hop through the clustered key, and every other index carries the clustered key. Use it for tables that grow large. PostgreSQL needs none of this: it has no clustered index, and a version 7 Guid inserts at the right edge of its B-tree.

## Unique violations

`WithUniqueViolation()` (or `[WithUniqueViolation]`) enables the decorator for duplicate-key errors **`2601`** (unique index) and **`2627`** (unique constraint or primary key) (`SqlServerErrorNumbers`; inherit it to add your own). It answers like the Postgres package: the error registered for the index in `AddUniqueViolationErrors`, or `database.conflict` (409) with the index's fields as details. The SQL error text is logged, never sent.

**The server must answer in English.** SQL Server gives the violated index name only inside its message text, and the decorator reads it from there. In another language the name is not found and the answer is a plain `database.conflict` without the registered error or fields. See [unique violations](./unique-violations.md).

## Health check

```csharp
services.AddSqlServerHealthCheck(appSettings.SqlServer);
```

Adds a check named `sqlserver` that runs `SELECT 1`, tagged `readiness`, `type-database` and `critical` ([health checks](../aspnetcore/health-checks.md)).

## Tests

The package's tests run on a real SQL Server ([testing](../instructions/testing.md)).
