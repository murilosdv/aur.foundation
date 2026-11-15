[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Entities

The base types for what a service stores. They carry no persistence code: the [Entity Framework Core package](../entity-framework/README.md) acts on them through the interfaces.

| Type | Gives |
|---|---|
| `IEntity` / `IEntity<TKey>` | An `Id` (`Guid` for `IEntity`; any struct key for `IEntity<TKey>`). |
| `ITimestamped` | `CreatedAt` (`init`) and `UpdatedAt`. The persistence layer fills both on save, so application code never sets them. |
| `ISoftDeletable` | `DeletedAt`. Removing the entity marks it instead of deleting; queries leave it out unless asked. Setting it back to `null` restores it. |
| `Entity` | `IEntity` + `ITimestamped`. |
| `SoftDeletableEntity` | `Entity` + `ISoftDeletable`. |

```csharp
public sealed class Post : SoftDeletableEntity
{
    public required string Slug { get; set; }
    public required string Title { get; set; }
}
```

## The id

`Entity.Id` is a **version 7 `Guid` created by the application** (`Guid.CreateVersion7()`) when the object is constructed. Two consequences:

- The id exists **before the insert**, so you can build a response, an event or related entities without a round trip.
- Version 7 ids are time-ordered: ids created later sort later. In PostgreSQL that inserts at the right edge of the index. SQL Server needs more help, see [SQL Server](../entity-framework/sqlserver.md#guid-keys-and-the-clustered-index).

## Timestamps

`CreatedAt` and `UpdatedAt` are `DateTimeOffset`. They come from the `TimeProvider` registered in the application (so tests can fix the clock), through the `TimestampInterceptor`. See [conventions and interceptors](../entity-framework/conventions-and-interceptors.md).

## Soft delete

Deleting a `SoftDeletableEntity` sets `DeletedAt` and keeps the row. A global query filter hides deleted rows everywhere; `IgnoreQueryFilters()` shows them. Unique indexes should be filtered with `WhereNotDeleted()` so a deleted row gives its value back.

Soft delete is **opt-in per entity** because a filtered parent with unfiltered children makes joins return nothing: an entity that others reference without the filter should stay a plain `Entity`.
