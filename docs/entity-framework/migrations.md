[Home](../../README.md) · [Documentation](../README.md) · [Entity Framework Core](./README.md)

# Migrations

Migrations live in a **project of their own** that only the EF tools use (for example `tools/Tools.Migrations`). The application never references it, so the migration code is not shipped, and the tools do not need to start the host (and its telemetry, authentication and the rest).

```
tools/Tools.Migrations/
  MigrationContext.cs     the design-time factory (below)
  History/                the generated migrations and the model snapshot
```

The project references the application's infrastructure project (where the `DbContext` and configurations are) and the provider package.

## The connection string

A design-time factory builds the context from a connection string taken as the **first argument after `--`**, so nothing is hard-coded and the same tool works against any environment:

```
dotnet ef migrations add InitialDb --project tools/Tools.Migrations --output-dir History -- "<connection string>"
dotnet ef migrations remove        --project tools/Tools.Migrations -- "<connection string>"
dotnet ef migrations script        --project tools/Tools.Migrations -- "<connection string>"
dotnet ef migrations has-pending-model-changes --project tools/Tools.Migrations -- "<connection string>"
dotnet ef database update          --project tools/Tools.Migrations -- "<connection string>"
```

`PostgresOptions.ToConnectionString()` builds the string from the same settings the service uses.

## The history table

The history table is built apart from `OnModelCreating`, so the naming convention would not reach it:

- **Postgres:** `__EFMigrationsHistory` keeps its Npgsql name but gets snake case columns and key (`UseSnakeCaseNpgsqlHistoryTable`).

## Practices

- Keep the migration project in a solution folder for tools, and check `has-pending-model-changes` in CI so a model change cannot be merged without its migration.
- Apply migrations as a **deployment step** (a script from `migrations script`, or `database update`), not when the service starts: the service should not need schema-altering rights.
- Review the generated SQL of every migration, especially index changes on large tables.
