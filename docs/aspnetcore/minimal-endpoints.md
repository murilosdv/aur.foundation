[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Minimal endpoints

Endpoints are declared as classes and discovered at startup, so the structure of the API is plain code that is validated before the first request:

- a **`ResourceGroup`** is one URL group (`/v1/posts`): its name, tags, versions, authorization and extra documented problems;
- a **`MinimalEndpoint<TGroup>`** is one endpoint of that group: its version, summary and the route and handler.

```csharp
builder.AddStandardMinimalEndpoints<Program>(out var endpoints);    // discover and validate (any type of the assembly)
app.MapStandardMinimalEndpoints(endpoints);                          // map them
```

The `endpoints` **catalog** is the single source of truth: mapping, API versioning and the OpenAPI documents all read it, so they cannot disagree.

## A resource group

```csharp
internal sealed class PostsResource : ResourceGroup
{
    public override string[] Tags => ["Blog"];
    public override MinimalEndpointVersion[] Versions => [1, new(2, IsDeprecated: true, Sunset: new DateOnly(2027, 1, 1), Description: "Use v3.")];
    public override int[] Problems => [404, 409];

    public override void Configure(RouteGroupBuilder group)
    {
        group.RequireScopes("api.write");     // narrows every endpoint of the group
    }
}
```

| Member | Default | Meaning |
|---|---|---|
| `TypeName` | class name without `Resource` | |
| `Name` | `TypeName` in slug case (`BlogPosts` → `blog-posts`) | The URL segment. Unique across the app. |
| `PrefixWithApi` | `false` | `true` makes the route `api/v{version}/{name}` instead of `v{version}/{name}`. |
| `RequireAuthorization` | `true` | Whether callers must be authenticated. **Default is closed.** |
| `Tags` | `[TypeName]` | Groups the endpoints in the API document. |
| `Versions` | `[1]` | The versions the group exposes (below). |
| `Problems` | `[]` | Extra problem status codes documented for this group, on top of the global ones. |
| `Configure(group)` | nothing | Adds conventions to every endpoint of the group: authorization, rate limiting, caching, … |

Groups and endpoints are created **once at startup** with their public parameterless constructors: they cannot take services. Handlers get services through their parameters.

## An endpoint

```csharp
internal sealed class GetPostEndpoint : MinimalEndpoint<PostsResource>
{
    public override string Summary => "Gets a post";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("{slug}", async (string slug, GetPost feature, CancellationToken ct) =>
            (await feature.HandleAsync(new GetPostRequest(slug), ct)).ToHttpResult())
            .RequireScopes("api.read")
            .Produces<PostResponse>();
    }
}
```

| Member | Default | Meaning |
|---|---|---|
| `Version` | `1` | Which API version this endpoint belongs to. The same route can exist in several versions, in different classes. |
| `Summary` | class name without `Endpoint`, in title case | The short title in the API document. |
| `Description` | none | A longer description in the API document. |
| `Name` | derived from `Summary` (PascalCase) | The route name. Unique across the app; identifies the endpoint for link generation and in the documents. |
| `Map(builder)` | required | Declares the route and handler on the group and returns the `RouteHandlerBuilder` so you can chain conventions. |

No registration is needed: the class existing is the registration.

## Conventions stack

Each level only **adds**; the requirements of every level must all pass.

| Level | Where | Typical use |
|---|---|---|
| Application | `app.MapStandardMinimalEndpoints(endpoints, api => api.…)` | Something every endpoint needs. |
| Resource | `ResourceGroup.Problems` and `Configure(group)` | Authorization, limits of one resource. |
| Endpoint | the chain after `Map…` | Scopes, idempotency, the response type. |

### Authorization

Groups require an authenticated caller by default, so a **new endpoint fails closed**. Narrow it with `group.RequireScopes("api.write")` in `Configure` or per endpoint (all levels must pass; see [authentication](./authentication.md#requiring-roles-and-scopes)). Opt out with `RequireAuthorization => false` on the group or `.AllowAnonymous()` on the endpoint.

### Documented problems

`MinimalApiConfiguration.StandardProblems` documents `401`, `403`, `429` and `500` on **every** endpoint (the `400` validation problem, with its `errors` dictionary, is documented separately), once for the whole API. Pass your own list to replace them:

```csharp
app.MapStandardMinimalEndpoints(endpoints, globalProblems: [401, 403, 429, 503]);
```

Add more per group (`Problems`) or per endpoint (`.ProducesProblem(404)`).

Paged endpoints use `.ProducesPage<T>()` / `.ProducesCursorPage<T>()` ([pagination](../core/pagination.md)).

### Validation of the request shape

`MapStandardMinimalEndpoints` also applies the validation filter to every endpoint ([validation](../core/validation.md)).

## Versions

Versions are in the URL (`/v1/…`, `/v2/…`). A version is a plain number, or a `MinimalEndpointVersion` with details:

```csharp
[1, new(2, IsDeprecated: true, Sunset: new DateOnly(2027, 1, 1), Description: "Use v3.")]
```

- A **deprecated** version is flagged in the response headers (`api-deprecated-versions`) and in the documents. `Sunset` needs `IsDeprecated`; it becomes a `Sunset` header policy with that date.
- `Description` is shown in the documents.
- Responses report the supported versions (`api-supported-versions`).

`services.AddStandardApiVersioning(endpoints)` registers the versioning (URL segment reader, default version 1, sunset policies, API explorer with group names `v1`, `v2`, … and the version substituted in the route).

### Adding a version

Add it to the group's `Versions` (mark the old one deprecated), then create endpoints with `Version => 2`. The OpenAPI document for `v2` appears by itself.

## What is checked at startup

The application **fails to start** when:

- group names or route names repeat;
- a group has no versions, a version is zero or a version repeats;
- an endpoint targets a version its group does not declare;
- a declared version has no endpoint;
- groups that share a version disagree on its deprecation, sunset or description;
- a sunset date is set on a version that is not deprecated;
- a group or endpoint has no public parameterless constructor.
