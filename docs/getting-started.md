[Home](../README.md) · [Documentation](./README.md)

# Getting started

This page builds a service from nothing and shows where each package plugs in. The [API archetype](https://github.com/murilosdv/aur.archetypes.api) is the complete, tested version of it: start from a copy of that when you can.

## Packages

| You need | Reference |
|---|---|
| Results, features, validation, pagination, settings models | `Aurore.Foundation.Core` |
| An HTTP service: authentication, request pipeline, endpoints, health, HTTP clients | `Aurore.Foundation.AspNetCore` |
| OpenAPI documents (and a UI) | `Aurore.Foundation.OpenApi`, plus `…OpenApi.Scalar` or `…OpenApi.Swagger` |
| Logs, metrics and traces | `Aurore.Foundation.Telemetry` |
| A database | `Aurore.Foundation.EntityFrameworkCore.Postgres` or `…SqlServer` (each brings `…EntityFrameworkCore`) |

Business code (features, entities, errors) should reference **only Core** and the EF package, so it stays free of ASP.NET Core and can run in a worker or a test.

## A layout that fits

```
src/
  App.Api/      the host: Program.cs, endpoints, resource groups, appsettings
  Core/         settings class, entities, errors, constants  → Foundation.Core
  Features/     one class per use case                       → Core, Infra
  Infra/        DbContext, configurations, HTTP clients      → Foundation.EntityFrameworkCore.*, AspNetCore
tools/Tools.Migrations/
tests/
```

## `Program.cs`

```csharp
WebApplication app;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .ConfigureStandardSerializers()                                              // JSON rules
        .LoadAppSettings(out var appSettings)                                        // GetStrict + validate (your code)
        .ConfigureStandardHosting(appSettings.Hosting)                               // shutdown timeout
        .AddStandardTelemetry(appSettings.AppInformation, appSettings.Telemetry)     // logs, metrics, traces
        .AddStandardMinimalEndpoints<Program>(out var endpoints)                     // discover resource groups and endpoints
        .AddAppApiDocs(appSettings, endpoints)                                       // OpenAPI + UI (your code)
        .AddStandardRequestPipeline(appSettings.RequestPipeline);                    // context, errors, limits, CORS, caches…

    AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);            // the shared ActivitySource

    builder.Services
        .AddSingleton(TimeProvider.System)
        .AddSingleton(appSettings.Pagination)
        .AddStandardApiVersioning(endpoints)
        .AddStandardAuthConfiguration(appSettings.IdentityProvider, appSettings.ApiKeys)
        .AddStandardHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.HttpDependencies)
        .AddStandardValidators([typeof(IApp).Assembly])
        .AddFeaturesFrom<IFeatures>(f => f.WithTelemetry().WithLogging().WithValidation().WithUniqueViolation())
        .AddStandardDbContext<AppDbContext>(appSettings.Postgres)
        .AddPostgresHealthCheck(appSettings.Postgres);

    app = builder.Build();

    app
        .UseStandardRequestPipeline(appSettings.RequestPipeline)    // forwarded headers → context → errors → headers → compression → CORS
        .UseAuthentication()
        .UseAuthorization()
        .UseStandardEndpointPolicies();                             // request user → rate limit → timeouts → output cache

    app.MapAppApiDocs(appSettings);                                 // non-production only
    app.MapStandardHealthChecks(appSettings.AppInformation);
    app.MapStandardMinimalEndpoints(endpoints);
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application failed to start: {ex}");
    return ExitCodes.ConfigurationError;                            // 78: a bad configuration stops the process before it serves anything
}

try
{
    app.Run();
    return ExitCodes.Success;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application stopped unexpectedly: {ex}");
    return ExitCodes.RuntimeError;                                  // 70
}
```

What each line is, and where to read about it:

| Call | Guide |
|---|---|
| `ConfigureStandardSerializers`, `PlatformNames.UsePrefix` | [Serialization](./core/serialization.md), [Constants](./core/constants.md#platformnames) |
| `LoadAppSettings` (`GetStrict` + validators) | [Settings](./aspnetcore/settings.md) |
| `ConfigureStandardHosting`, `AppDiagnostics`, `ExitCodes` | [Application](./core/application.md) |
| `AddStandardTelemetry` | [Telemetry](./telemetry/README.md) |
| `AddStandardMinimalEndpoints`, `MapStandardMinimalEndpoints`, `AddStandardApiVersioning` | [Minimal endpoints](./aspnetcore/minimal-endpoints.md) |
| API documentation | [OpenAPI](./openapi/README.md), [Scalar](./openapi/scalar.md), [Swagger](./openapi/swagger.md) |
| `AddStandardRequestPipeline`, `UseStandardRequestPipeline`, `UseStandardEndpointPolicies` | [Request pipeline](./aspnetcore/request-pipeline.md), [settings](./aspnetcore/request-pipeline-settings.md) |
| `AddStandardAuthConfiguration` | [Authentication](./aspnetcore/authentication.md) |
| `AddStandardHealthChecks`, `MapStandardHealthChecks` | [Health checks](./aspnetcore/health-checks.md) |
| `AddStandardValidators` | [Validation](./core/validation.md) |
| `AddFeaturesFrom` | [Features](./core/features.md) |
| `AddStandardDbContext`, `AddPostgresHealthCheck` | [Entity Framework Core](./entity-framework/README.md) |

If `PlatformNames.UsePrefix(...)` is used, it is the **first line** of the file.

## The settings file

A minimal `appsettings.json` for the program above:

```json
{
  "AppInformation": {
    "Domain": "Acme", "Boundary": "Billing", "Component": "InvoiceApi",
    "Description": "Issues and tracks invoices.",
    "Maintainer": { "Name": "billing-team", "Email": "billing@example.com" }
  },
  "IdentityProvider": { "AuthorityUrl": "http://127.0.0.1:8080/realms/acme", "Audience": "api", "RequireHttpsMetadata": false },
  "Telemetry": { "Otlp": { "Endpoint": "http://localhost:4317", "Protocol": "Grpc" } },
  "Postgres": { "Host": "127.0.0.1", "Database": "invoices", "Username": "postgres", "Password": "postgres" }
}
```

Everything else has a default. Production values and every secret come from `appsettings.Production.json` and environment variables; the validators refuse development values in production.

## A resource, an endpoint and a feature

```csharp
internal sealed class InvoicesResource : ResourceGroup { }                    // /v1/invoices, authenticated by default

internal sealed class GetInvoiceEndpoint : MinimalEndpoint<InvoicesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder) =>
        builder.MapGet("{id:guid}", async (Guid id, GetInvoice feature, CancellationToken ct) =>
            (await feature.HandleAsync(new GetInvoiceRequest(id), ct)).ToHttpResult())
            .RequireScopes("api.read")
            .Produces<InvoiceResponse>();
}

public sealed class GetInvoice(AppDbContext db) : Feature<GetInvoiceRequest, InvoiceResponse>
{
    protected override async ValueTask<Result<InvoiceResponse>> ExecuteAsync(GetInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return invoice is null ? AppErrors.NotFound : InvoiceResponse.From(invoice);
    }
}
```

No registration of the endpoint or the feature is needed; the scans find them.

## Where to go next

- Understand how failures flow: [results and errors](./core/results-and-errors.md), [results to HTTP](./aspnetcore/results-to-http.md).
- Add lists: [pagination](./core/pagination.md).
- Make writes retry-safe: [idempotency](./aspnetcore/idempotency.md), [unique violations](./entity-framework/unique-violations.md).
- Run it in a container behind a proxy: set `ForwardedHeaders:KnownNetworks`, the privacy key, a production authority and the database password through the environment.
