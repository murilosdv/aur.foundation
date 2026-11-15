[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Settings: binding and validation

A service reads one settings class, binds it **strictly**, validates it **once at startup**, and hands the parts to the `Add…`/`Use…` methods that need them. The foundation provides the pieces; the application writes the class.

## The application's settings class

```csharp
public sealed record AppSettings
{
    public required AppInformationOptions AppInformation { get; init; }
    public ApiDocsOptions ApiDocs { get; init; } = new();
    public required IdentityProviderOptions IdentityProvider { get; init; }
    public ApiKeysOptions ApiKeys { get; init; } = new();
    public required TelemetryOptions Telemetry { get; init; }
    public required PostgresOptions Postgres { get; init; }
    public HealthChecksOptions HealthChecks { get; init; } = new();
    public HostingOptions Hosting { get; init; } = new();
    public RequestPipelineOptions RequestPipeline { get; init; } = new();
    public PaginationOptions Pagination { get; init; } = new();
    public IReadOnlyDictionary<string, HttpDependencyOptions> HttpDependencies { get; init; } = new Dictionary<string, HttpDependencyOptions>();
}
```

`required` sections must be present in the configuration; the others have defaults and can be absent.

| Section | Type | Guide |
|---|---|---|
| `AppInformation` | `AppInformationOptions` | [Application](../core/application.md) |
| `ApiDocs` | `ApiDocsOptions` | [Application](../core/application.md#apidocs) |
| `Hosting` | `HostingOptions` | [Application](../core/application.md#hosting) |
| `IdentityProvider`, `ApiKeys` | `IdentityProviderOptions`, `ApiKeysOptions` | [Authentication](./authentication.md) |
| `RequestPipeline` | `RequestPipelineOptions` | [Request pipeline settings](./request-pipeline-settings.md) |
| `Pagination` | `PaginationOptions` | [Pagination](../core/pagination.md#settings) |
| `HealthChecks` | `HealthChecksOptions` | [Health checks](./health-checks.md) |
| `HttpDependencies` | dictionary of `HttpDependencyOptions` | [HTTP dependencies](./http-dependencies.md) |
| `Telemetry` | `TelemetryOptions` | [Telemetry](../telemetry/README.md) |
| `Postgres` / `SqlServer` | `PostgresOptions` / `SqlServerOptions` | [Postgres](../entity-framework/postgres.md), [SQL Server](../entity-framework/sqlserver.md) |

## `GetStrict<T>()`

```csharp
var appSettings = builder.Configuration.GetStrict<AppSettings>();
```

Binds like `Get<T>()` but **fails on keys it does not know**, so a typo in `appsettings.json` is an error instead of a silently ignored value (`RateLimiting:Policies:std:PermitLimt` would otherwise fall back to the default). Only the top-level sections the class declares are considered; the rest (`Logging`, `AllowedHosts`, other environment variables) belongs to the framework. It throws `InvalidOperationException` when nothing is configured for the class.

## Validating

Write one validator for your settings class that composes the foundation's, so every problem is reported together with the key it belongs to, and the production rules apply only in production:

```csharp
public sealed class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator(IHostEnvironment environment)
    {
        var production = environment.IsProduction();

        // AppInformation, Hosting and HealthChecks have no validator in the foundation: state the rules you need.
        RuleFor(x => x.AppInformation.Domain).NotEmpty();
        RuleFor(x => x.AppInformation.Maintainer.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Hosting.ShutdownTimeoutSeconds).GreaterThan(0);
        RuleFor(x => x.HealthChecks.CacheSeconds).GreaterThanOrEqualTo(0);

        RuleFor(x => x.IdentityProvider).SetValidator(new IdentityProviderOptionsValidator(production));
        RuleFor(x => x.ApiKeys).SetValidator(new ApiKeysOptionsValidator(production));
        RuleFor(x => x.RequestPipeline).SetValidator(new RequestPipelineOptionsValidator());
        RuleFor(x => x.Pagination).SetValidator(new PaginationOptionsValidator());
        RuleFor(x => x.Telemetry).SetValidator(new TelemetryOptionsValidator(production));
        RuleFor(x => x.Postgres).SetValidator(new PostgresOptionsValidator(production));

        // A dictionary: report each failure with its configuration key (HttpDependencies:Orders:BaseUrl), not a position.
        RuleFor(x => x.HttpDependencies).Custom((dependencies, context) =>
        {
            var validator = new HttpDependencyOptionsValidator(production);
            foreach (var (name, dependency) in dependencies)
                foreach (var failure in validator.Validate(dependency).Errors)
                    context.AddFailure($"HttpDependencies:{name}:{failure.PropertyName}", failure.ErrorMessage);
        });
    }
}

new AppSettingsValidator(builder.Environment).ValidateAndThrow(appSettings);
```

| Validator | Production rules |
|---|---|
| `IdentityProviderOptionsValidator(isProduction)` | No local or `http` authority; `RequireHttpsMetadata` must be true. |
| `ApiKeysOptionsValidator(isProduction)` | Keys of at least 32 characters. |
| `RequestPipelineOptionsValidator()` | (same everywhere) |
| `TelemetryOptionsValidator(isProduction)` | No local collector endpoint. |
| `HttpDependencyOptionsValidator(isProduction)` | No local base or token URL. |
| `PostgresOptionsValidator(isProduction)` / `SqlServerOptionsValidator(isProduction)` | No local host; SQL Server requires encryption and a validated certificate. |
| `PaginationOptionsValidator()` | (same everywhere) |

These validators are plain FluentValidation validators. They are **not** registered in the container: create them by hand at startup (`AddStandardValidators` skips them on purpose).

## Passing, not registering

Do not register the settings object in the container: pass the parts to the methods that need them (`AddStandardTelemetry(appSettings.AppInformation, appSettings.Telemetry)`). Configuration then does not stay reachable for the lifetime of the app.

## Environments

`appsettings.json` holds the **development** values. Production and staging files hold only what differs; everything else, and every secret, comes from environment variables with `__` for nesting:

```
Postgres__Password=…
ApiKeys__Clients__billing-service__Key=…
RequestPipeline__Privacy__Keys__Current=…
Telemetry__Otlp__Endpoint=https://collector.example.com:4317
```

An invalid value stops the process before it serves a request, with exit code `78` ([`ExitCodes`](../core/application.md#exitcodes)).

## Hosting and serializers

`builder.ConfigureStandardHosting(appSettings.Hosting)` applies the shutdown timeout. `builder.ConfigureStandardSerializers()` makes the minimal API's JSON follow the [shared rules](../core/serialization.md).
