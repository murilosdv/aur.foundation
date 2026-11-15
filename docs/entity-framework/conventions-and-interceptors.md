[Home](../../README.md) · [Documentation](../README.md) · [Entity Framework Core](./README.md)

# Conventions and interceptors

## `ApplyStandardConventions()`

Call it last in `OnModelCreating`. It runs two steps, which can also be called alone.

### Soft delete filter: `ApplySoftDeleteFilters()`

Adds a global query filter named `QueryFilters.SoftDelete` (`"soft-delete"`) to every `ISoftDeletable` entity: `DeletedAt == null`. Deleted rows disappear from every query, including `Include`s.

```csharp
db.Posts.IgnoreQueryFilters()                                  // everything, deleted too
db.Posts.IgnoreQueryFilters([QueryFilters.SoftDelete])         // ignore only this filter, keep your own
```

### Snake case: `UseSnakeCaseNamingConvention()`

Writes schemas, tables, columns, keys, foreign keys and indexes in snake case, on every provider (`CreatedAt` → `created_at`, `PK_Posts` → `pk_posts`). An index you name yourself (`HasDatabaseName`) is also converted, so use the snake case name in the [unique violation map](./unique-violations.md).

## `EntityConfigurationBase<TEntity>`

The base of an entity's configuration (`EntityConfigurationBase<TEntity, TKey>` for a non-`Guid` key):

| Member | Default | |
|---|---|---|
| `SchemaName` | `app` (`DatabaseDefaults.Schema`) | The table's schema. Override to change. |
| `TableName` | the class name in the plural, snake case (`Person` → `people`; a trailing `Entity` is dropped first: `PostEntity` → `posts`) | Override to change. |
| primary key | `pk_<table>` | |
| `Extend(builder)` | nothing | Configure everything else here. |

Do not override `Configure`; `Extend` is where yours goes.

## Interceptors

Both are registered by `AddStandardDbContext`, in this order, and both need a `TimeProvider` in the container.

| Interceptor | Effect |
|---|---|
| `SoftDeleteInterceptor` | `Remove` on an `ISoftDeletable` becomes an update of **`DeletedAt` only**, so a concurrent change to other columns is not overwritten. Setting `DeletedAt` back to `null` restores the entity. |
| `TimestampInterceptor` | Sets `CreatedAt` and `UpdatedAt` on added entities and `UpdatedAt` on modified ones, from the `TimeProvider`. Application code never sets them. |

Soft delete runs first, so a deletion also moves `UpdatedAt`. Inject a fixed `TimeProvider` (`FakeTimeProvider`) in tests to get predictable values.

### What they do not cover

- **`ExecuteDelete` and `ExecuteUpdate`** (bulk statements) bypass the change tracker: they delete or update for real and do not touch timestamps.
- **Cascade deletes** remove related rows for real unless those are soft deletable too.
- Raw SQL, of course.

Design with this in mind: an entity that others reference without the soft delete filter should stay a plain `Entity` (a filtered parent with unfiltered children makes joins return nothing).

## Queries are no-tracking by default

`AddStandardDbContext` turns tracking off. Load with `AsTracking()` the entities you are going to change and save. Read-only queries (the common case) then cost less and cannot accidentally persist a change.

## Decimals

`builder.Property(x => x.Price).AsDecimalColumn()` is `decimal(10, 2)`; pass `precision` and `scale` for others. Leaving a decimal without a type makes EF choose a default and warn.
