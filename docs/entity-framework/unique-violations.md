[Home](../../README.md) · [Documentation](../README.md) · [Entity Framework Core](./README.md)

# Unique violations

When two requests create the same value at the same moment, only the database can guarantee uniqueness: both can pass a "does it exist?" check and both try to insert. The loser would normally become a 500. With the **unique violation decorator** it becomes a `409 Conflict` that says what clashed.

## Enabling it

Per registration (every feature) or per feature:

```csharp
services.AddFeaturesFrom<IFeatures>(features => features
    .WithTelemetry().WithLogging().WithValidation()
    .WithUniqueViolation());                       // from the Postgres or SqlServer package

[WithUniqueViolation]                              // or on one feature
public sealed class CreatePost(...) : Feature<...> { ... }
```

The decorator ([features](../core/features.md)) catches the provider's duplicate-key error while the feature saves, and returns a failed result instead of the exception.

| Provider | Detects | Where |
|---|---|---|
| PostgreSQL | SQLSTATE `23505` (`PostgresSqlStates.UniqueViolation`) | [Postgres](./postgres.md#unique-violations) |
| SQL Server | error `2601` (unique index) and `2627` (unique constraint or primary key) (`SqlServerErrorNumbers`) | [SQL Server](./sqlserver.md#unique-violations) |

Inherit `PostgresSqlStates` / `SqlServerErrorNumbers` to name other codes your application handles.

## What the client receives

If you registered an error for the violated index (below), that error. Otherwise the generic `database.conflict` (409, "The request conflicts with data that already exists.") with the violated **fields as details**, read from the model:

```json
{ "status": 409, "code": "database.conflict", "errors": { "email": ["A record with this value already exists."] } }
```

**The constraint's name is never sent to the client.** For SQL Server the SQL error text is logged, never sent.

## The map: a specific error for a specific index

```csharp
services.AddUniqueViolationErrors(map => map
    .On(AuthorConfiguration.UniqueHandleIndex, AppErrors.Authors.HandleTaken)
    .On(PostConfiguration.UniqueSlugIndex, AppErrors.Posts.SlugTaken));
```

`On(constraintName, error)` answers a violation of that constraint with your error (your own code, message and kind). The name is the **database name of the unique index, in snake case once the conventions ran**: give the index an explicit name with `HasDatabaseName("ux_posts_slug")` and keep it in a constant that both the configuration and the map use. A constraint with no entry answers `database.conflict`.

## Practical notes

- **Postgres aborts the transaction** on a violation. A feature that keeps running statements in the same transaction afterwards must check first (or not share the transaction).
- Checking first (a query before the insert) still gives a friendlier message in the common case, and the decorator covers the race. Do both when the message matters.
- Unique indexes on soft deletable entities should be filtered with `WhereNotDeleted()` so a deleted row's value can be reused ([conventions](./conventions-and-interceptors.md)).
- **SQL Server must answer in English.** It gives the violated index name only inside its message text, and the decorator reads it from there. In another language the name is not found and the answer is a plain `database.conflict` without the registered error or the fields.
