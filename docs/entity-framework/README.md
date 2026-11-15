[Home](../../README.md) · [Documentation](../README.md)

# Entity Framework Core

`Aurore.Foundation.EntityFrameworkCore` holds what every database shares: conventions (snake case, schema, soft delete filter), the interceptors that fill timestamps and soft-delete, asynchronous paging and the answer to a unique-constraint violation. It **knows no database**: add one provider package.

| Package | Database | Guide |
|---|---|---|
| `Aurore.Foundation.EntityFrameworkCore.Postgres` | PostgreSQL (Npgsql) | [Postgres](./postgres.md) |
| `Aurore.Foundation.EntityFrameworkCore.SqlServer` | SQL Server (Microsoft.Data.SqlClient) | [SQL Server](./sqlserver.md) |

```
dotnet add package Aurore.Foundation.EntityFrameworkCore.Postgres     # brings the base package
```

Everything on these pages is the same on both providers unless a provider page says otherwise.

## Pages

| Page | Covers |
|---|---|
| This page | Entities, registering the context, the model. |
| [Conventions and interceptors](./conventions-and-interceptors.md) | Snake case, the `app` schema, soft delete filter, `EntityConfigurationBase`, timestamps, bulk operations. |
| [Unique violations](./unique-violations.md) | Answering a duplicate with a conflict instead of a 500; the constraint map. |
| [Paging](../core/pagination.md#building-the-page) | `ToPageAsync` and `ToCursorPageAsync`. |
| [Migrations](./migrations.md) | A migrations project of its own and the design-time factory. |

## Entities

Entities live in application code and use the types in `Aurore.Foundation.Core.Entities` ([entities](../core/entities.md)): `Entity` (version 7 `Guid` id, `CreatedAt`, `UpdatedAt`) and `SoftDeletableEntity` (plus `DeletedAt`). Everything here acts **only on entities that implement the matching interface**; a plain class is left alone.

## Registering the context

The provider package has the overload that takes its connection settings:

```csharp
services.AddSingleton(TimeProvider.System);                                  // the interceptors read the time from here
services.AddStandardDbContext<AppDbContext>(appSettings.Postgres);           // or appSettings.SqlServer
```

It registers the context with:

- the provider and the connection built from the settings;
- **no-tracking queries by default** (use `AsTracking()` on a query that loads entities you will change);
- the two interceptors, in the order they must run (soft delete first, then timestamps).

A plain `AddDbContext` has none of these. To choose the provider yourself, use the base overload `AddStandardDbContext<TContext>(builder => builder.UseNpgsql(connectionString))`; a provider overload also takes an optional `configure` action to adjust the options builder further.

## The context

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfigurationsFromAssembly(typeof(IInfra).Assembly)   // your IEntityTypeConfiguration classes
            .ApplyStandardConventions();                                // LAST, after the configurations
    }
}
```

`ApplyStandardConventions()` must come after the entity configurations: it applies to what they declared. What it does is on [conventions and interceptors](./conventions-and-interceptors.md).

## An entity configuration

```csharp
internal sealed class PostConfiguration : EntityConfigurationBase<Post>
{
    public const string UniqueSlugIndex = "ux_posts_slug";

    protected override void Extend(EntityTypeBuilder<Post> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(BlogRules.TitleMaxLength);
        builder.HasIndex(x => x.Slug).IsUnique().WhereNotDeleted().HasDatabaseName(UniqueSlugIndex);
    }
}
```

`EntityConfigurationBase<TEntity>` names the table (plural, in the `app` schema) and the primary key and gives you `Extend` for the rest. Details in [conventions](./conventions-and-interceptors.md#entityconfigurationbasetentity).

## Column helpers

| Helper | Package | Effect |
|---|---|---|
| `AsDecimalColumn(precision = 10, scale = 2)` | base | Decimal type with explicit precision and scale. |
| `AsTimestampColumn(...)` | each provider | A date-time column of the provider's right type (`timestamptz` / `datetimeoffset`). |
| `WhereNotDeleted()` | each provider | Makes an index partial/filtered on `deleted_at IS NULL`, so a deleted row gives its value back. It is SQL of that database, which is why it is in the provider. |

## Health check

Each provider adds a readiness check that runs `SELECT 1` ([Postgres](./postgres.md#health-check), [SQL Server](./sqlserver.md#health-check)); it feeds `/health/ready` ([health checks](../aspnetcore/health-checks.md)).

## Testing

The provider packages' own tests run on a **real PostgreSQL and a real SQL Server** (the EF tests are written once as abstract classes and run by each provider), because a fake database only proves the fake works. See [testing](../instructions/testing.md).
