[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Results and errors

A use case can fail in ways the caller should know about: the post does not exist, the slug is taken, the caller may not publish. These are not exceptions. They are **expected outcomes**, returned as a value the caller must look at.

## `Result` and `Result<T>`

```csharp
public async ValueTask<Result<Post>> GetAsync(Guid id)
{
    var post = await db.Posts.FindAsync(id);
    if (post is null)
        return AppErrors.NotFound;      // converts to a failed Result<Post>

    return post;                        // converts to a successful Result<Post>
}
```

| Member | Meaning |
|---|---|
| `Result.Success` | The success of an operation with no value (a command). |
| `Result.Failure(error)` | A failed result. |
| `Result<T>.Success(value)` / `Result<T>.Failure(error)` | The same, for an operation that produces a `T`. |
| `IsSuccess` / `IsFailure` | Which one it is. |
| `Error` | The `ApplicationError`, or `null` on success. |
| `Value` | The value of a successful `Result<T>`. **Reading it from a failed result throws**: that is a bug in the caller, which forgot to check. |

Implicit conversions let a method return a value or an error directly (`return post;`, `return AppErrors.NotFound;`) instead of calling a factory.

## `ApplicationError`

```csharp
public static readonly ApplicationError SlugTaken =
    ApplicationError.Conflict("post.slug_taken", "A post with this slug already exists.");
```

| Property | Meaning |
|---|---|
| `Code` | The **stable key clients rely on**, written `area.snake_case_name` (`post.slug_taken`). Never change it once released: clients branch on it. |
| `Message` | The text a person reads. Can change freely. |
| `Kind` | What sort of failure it is; the web layer maps it to a status (table below). |
| `Details` | Extra information about one occurrence, usually field path to messages. Empty unless `WithDetails` was used. |

Declare each error **once, as a static field**, then return that instance. `WithDetails(details)` returns a copy carrying the details, so the shared instance is never modified.

### Kinds and the HTTP status they become

| Factory | `ErrorKind` | Meaning | HTTP |
|---|---|---|---|
| `ApplicationError.Validation` | `Validation` | The input is invalid. | 400 |
| `ApplicationError.NotFound` | `NotFound` | The requested thing does not exist. | 404 |
| `ApplicationError.Conflict` | `Conflict` | Conflicts with the current state: duplicate, stale data, wrong state. | 409 |
| `ApplicationError.Unauthorized` | `Unauthorized` | The caller is not authenticated. | 401 |
| `ApplicationError.Forbidden` | `Forbidden` | Authenticated, but not allowed. | 403 |
| `ApplicationError.Failure` | `Failure` | Something went wrong on our side. | 500 |
| `ApplicationError.Unavailable` | `Unavailable` | The service or a dependency is temporarily unavailable. | 503 |

`Failure` and `Unavailable` are also what the [telemetry](./features.md#telemetry) and [logging](./features.md#logging) decorators treat as errors on our side; the other kinds are normal answers.

## `StandardErrors` and your own `AppErrors`

`StandardErrors` is an abstract class with the errors every application shares:

| Field | Code | Kind |
|---|---|---|
| `Unexpected` | `general.unexpected` | Failure |
| `Validation` | `general.validation` | Validation |
| `NotFound` | `general.not_found` | NotFound |
| `Conflict` | `general.conflict` | Conflict |
| `Unauthorized` | `general.unauthorized` | Unauthorized |
| `Forbidden` | `general.forbidden` | Forbidden |

An application **derives from it**, so its own errors and the standard ones are reachable from one place:

```csharp
public sealed class AppErrors : StandardErrors
{
    public static class Posts
    {
        public static readonly ApplicationError SlugTaken =
            ApplicationError.Conflict("post.slug_taken", "A post with this slug already exists.");
    }
}

return AppErrors.NotFound;           // inherited
return AppErrors.Posts.SlugTaken;    // the application's own
```

## Field errors

`Validation` errors usually carry details: the field path (dotted camelCase, `items.0.name`) and the messages for it. You rarely build them by hand; [validation](./validation.md) produces them from FluentValidation failures. By hand:

```csharp
return AppErrors.Validation.WithDetails(new Dictionary<string, string[]>
{
    ["email"] = ["Must be a valid address."],
});
```

## From a result to an HTTP response

The ASP.NET Core package turns a result into a response with `ToHttpResult()`; see [Results to HTTP](../aspnetcore/results-to-http.md) for the status table, the body of a problem and how to choose the success status.

## Exceptions

Unexpected exceptions are not caught in features. In HTTP the exception handler of the [request pipeline](../aspnetcore/request-pipeline.md#3-errors) answers a generic 500 problem (never the exception message) with the correlation id, and the logging decorator records which feature threw before rethrowing.
