[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Features and decorators

A **feature** is one use case: a class with a request, a result and the logic in between. **Decorators** wrap the run of a feature to add cross-cutting behavior (a trace span, a log line, validation) without the feature knowing.

## Writing a feature

```csharp
public sealed class GetPost(AppDbContext db) : Feature<GetPostRequest, PostResponse>
{
    protected override async ValueTask<Result<PostResponse>> ExecuteAsync(GetPostRequest request, CancellationToken cancellationToken)
    {
        var post = await db.Posts.FirstOrDefaultAsync(x => x.Slug == request.Slug, cancellationToken);

        return post is null ? AppErrors.NotFound : PostResponse.From(post);
    }
}

public sealed record GetPostRequest(string Slug);
```

| Base class | Use it for | You call | You write |
|---|---|---|---|
| `Feature<TRequest>` | A command: succeeds or fails, no value. | `HandleAsync(request, ct)` returns `ValueTask<Result>` | `ExecuteAsync` |
| `Feature<TRequest, TResponse>` | A query or a command that returns a value. | `HandleAsync(request, ct)` returns `ValueTask<Result<TResponse>>` | `ExecuteAsync` |

- You **inject the feature itself** (not an interface) where it is used: `GetPost` in the endpoint. Each feature is registered as its own type.
- `HandleAsync` runs the decorators and then `ExecuteAsync`. Built by hand (in a unit test, without the registration) a feature has no pipeline and `HandleAsync` runs the logic alone.
- The request type must be non-null. A record is the usual shape.

## Registering

```csharp
services.AddFeaturesFrom<IFeatures>(features => features
    .WithTelemetry()
    .WithLogging()
    .WithValidation());
```

`AddFeaturesFrom<TAssembly>` scans the assembly of `TAssembly` (any type in it; the archetype uses a marker interface `IFeatures`) for every non-abstract class deriving from `Feature<…>`. Parameters:

| Parameter | Meaning |
|---|---|
| `configure` | Chooses the decorators **enabled for every feature** of this registration. Nothing is enabled unless you say so. |
| `lifetime` | `ServiceLifetime.Scoped` by default. |

Because the set is chosen per registration, a worker that registers the same features can enable a different set than the API.

### The builder

| Method | Enables |
|---|---|
| `WithTelemetry()` | A span per run. |
| `WithLogging()` | One log line per run. |
| `WithValidation()` | The business validators of the request. |
| `With<TDecorator>()` | A decorator of your own. |
| `WithUniqueViolation()` | (EF Core packages) Answers a unique-constraint violation with a conflict. See [unique violations](../entity-framework/unique-violations.md). |

### Per feature: `With` and `Without`

A feature can add or remove a decorator with attributes, on top of what the registration enabled:

```csharp
[WithLogging]                          // adds logging for this feature only
[Without<TelemetryDecorator>]          // removes telemetry for this feature
[With<AuditDecorator>]                 // adds a decorator of your own
public sealed class DeletePost(...) : Feature<DeletePostRequest> { ... }
```

Ready-made: `[WithTelemetry]`, `[WithLogging]`, `[WithValidation]`, and `[WithUniqueViolation]` from the EF packages. **`Without` wins over `With`**, so a feature can switch off what the registration enabled for all.

## The built-in decorators

### Telemetry

Runs the feature inside a span named `feature <FeatureName>` with the tag `aur.feature.name`. When the result fails it adds the tag `aur.error.code`; only failures on our side (`Failure`, `Unavailable`) mark the span as an error, because a not found or a conflict is a normal answer. An exception marks the span as an error with the exception type and rethrows. The span lives on the application's shared source, so [`AppDiagnostics.Initialize`](./application.md#appdiagnostics) must run at startup and the source must be listened to ([telemetry](../telemetry/README.md)).

### Logging

One line per run, with the feature name and the time it took. The request is **never logged** because it can hold personal data.

| Outcome | Level | Message |
|---|---|---|
| Success | Information | `Feature {Feature} succeeded in {ElapsedMs} ms` |
| Failed with a client-side kind (validation, not found, conflict, unauthorized, forbidden) | Information | `Feature {Feature} answered {ErrorCode} in {ElapsedMs} ms` |
| Failed with `Failure` or `Unavailable` | Warning | `Feature {Feature} failed with {ErrorCode} in {ElapsedMs} ms` |
| Threw | Error | `Feature {Feature} threw {ExceptionType} after {ElapsedMs} ms` (then rethrown) |

### Validation

Runs every `IFeatureValidator<TRequest>` registered for the request and, if any rule fails, returns the failure **without running the feature**, so the feature only ever sees requests that passed. A request with no validator goes straight through. See [validation](./validation.md).

## Order

Decorators are sorted by `Order`: lower runs first (outermost).

| Decorator | Order | Constant |
|---|---|---|
| Telemetry | 100 | `FeatureOrder.Telemetry` |
| Logging | 200 | `FeatureOrder.Logging` |
| Validation | 300 | `FeatureOrder.Validation` |
| Your own, by default | 1000 | `FeatureOrder.Default` |

Telemetry is outermost so it measures everything. Inherit from `FeatureOrder` to name the places your application adds: `public sealed class AppFeatureOrder : FeatureOrder { public const int Audit = Validation + 50; }`.

## Writing a decorator

```csharp
public sealed class AuditDecorator(IAuditLog audit) : IFeatureDecorator
{
    public int Order => FeatureOrder.Default;

    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        var result = await next();                       // runs the rest of the pipeline and the feature
        await audit.WriteAsync(context.FeatureName, result.IsSuccess);
        return result;
    }
}

features.With<AuditDecorator>();
```

- A decorator is resolved from the container, so it can take services in its constructor.
- Not calling `next()` stops the feature from running. A failed `Result` returned this way is converted to the failure of the feature's own result type.
- `FeatureContext` tells you `FeatureType`, `FeatureName`, `RequestType`, `Request`, `Services` (the scope the feature runs in) and `CancellationToken`.

## Where features belong

Features hold the **business rules** and know nothing about HTTP. The endpoint validates the shape of the request, builds the feature request from the caller and the body, calls `HandleAsync` and returns `result.ToHttpResult()`. See the [archetype's guide](https://github.com/murilosdv/aur.archetypes.api/blob/main/docs/guides/adding-a-feature.md) for a worked example.
