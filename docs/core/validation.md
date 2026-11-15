[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Validation

Validation uses [FluentValidation](https://docs.fluentvalidation.net) and splits in two, because the two kinds of rule run in different places and answer differently.

| | Request validation | Business validation |
|---|---|---|
| Base class | `RequestValidator<T>` | `FeatureValidator<T>` |
| Marker interface | `IRequestValidator<T>` | `IFeatureValidator<T>` |
| Checks | The **shape** of the input: required, length, format, range. | **Rules** that need the domain or the database: "the author has a profile", "the title is unique". |
| Runs in | The HTTP validation filter, before the handler. | The feature pipeline (`WithValidation()`), before the feature. |
| Fails with | `400` validation problem with field details. | `400` by default, or the error you choose (for example a `409`). |

Keeping them apart means one validator never runs twice for the same request, and shape errors never reach the feature.

## Request validators

```csharp
public sealed class CreatePostBodyValidator : RequestValidator<CreatePostBody>
{
    public CreatePostBodyValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(BlogRules.TitleMaxLength);
        RuleFor(x => x.Slug).Matches(Patterns.Slug);
    }
}
```

The ASP.NET Core package's validation filter is applied to every endpoint by `MapStandardMinimalEndpoints`. For each handler parameter whose **declared type** has a registered `IRequestValidator<T>` it runs all of them (a second validator for the same type is not ignored) before the handler and answers an invalid request with the 400 problem. The decision is made once when the endpoint is built: endpoints with no validated parameter get no filter and cost nothing per request.

## Business validators

```csharp
public sealed class CreatePostValidator : FeatureValidator<CreatePostRequest>
{
    public CreatePostValidator(AppDbContext db)
    {
        RuleFor(x => x.AuthorId).MustAsync(async (id, ct) => await db.Authors.AnyAsync(a => a.Id == id, ct))
            .WithMessage("The author needs a profile.");
    }

    // Optional: answer this error instead of a plain 400.
    public override ApplicationError? Error => AppErrors.Authors.ProfileRequired;
}
```

`Error` decides the code, message and kind of the answer when a rule fails; the failed rules become its `Details`. Without it the failure is the standard 400 validation error.

Enable the decorator with `.WithValidation()` or `[WithValidation]` ([features](./features.md)). A request with no validator passes straight through.

## Registering

```csharp
services.AddStandardValidators([typeof(IApp).Assembly]);
```

Scans the assemblies and registers **only** validators that derive from `RequestValidator<T>` or `FeatureValidator<T>`, as scoped services, so they can take the `DbContext` or other scoped services. Any other validator (such as a plain `AbstractValidator` used for settings) is **not** registered: create it by hand where you need it. `includeInternalTypes` (default true) also registers `internal` validators.

## From failures to an error

`FluentValidationExtensions.ToApplicationError` turns failures into an `ApplicationError`:

```csharp
var error = validationResult.ToApplicationError();                    // standard 400
var error = validationResult.ToApplicationError(AppErrors.Conflict);  // another kind and code
```

- Messages are **grouped by field** in `Details`, de-duplicated.
- Field paths are **dotted camelCase with numeric indexes**: `Items[0].Name` becomes `items.0.name`, which is what form libraries use to find the field.

In an endpoint, `validationResult.ToHttpResult(error?)` does both steps and returns the problem response ([results to HTTP](../aspnetcore/results-to-http.md)).

## Settings validators

The options of the foundation (`RequestPipelineOptions`, `IdentityProviderOptions`, `ApiKeysOptions`, `HttpDependencyOptions`, `PaginationOptions`, `TelemetryOptions`, the database options) each have an `AbstractValidator` that **names the configuration key of every problem** and is stricter in production. They are created by hand at startup, not through the container; see [settings](../aspnetcore/settings.md).
