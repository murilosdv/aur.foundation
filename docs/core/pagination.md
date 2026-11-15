[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Pagination

Two kinds of list, one request model and one whitelist of sortable fields.

| | Numbered page | Cursor page |
|---|---|---|
| Request | `PageRequest` | `CursorRequest` |
| Response | `Page<T>`: `items`, `pageNumber`, `pageSize`, `totalItems`, `totalPages` | `CursorPage<T>`: `items`, `nextCursor`, `pageSize` (no total) |
| Use it for | A table with a pager and a total. | Infinite scroll, feeds, large or changing data. |
| Cost | A count on every page. | None; fetches one row more than the page to know if there is a next. |
| Cannot | Stay stable while rows are inserted. | Jump to page N. |

## Settings

```json
"Pagination": { "DefaultPageSize": 20, "MaxPageSize": 100 }
```

| Key | Default | Meaning |
|---|---|---|
| `DefaultPageSize` | 20 | Used when the request sends no `pageSize`. Must be greater than 0 and not above `MaxPageSize`. |
| `MaxPageSize` | 100 | The largest size a request may ask for. A larger value is answered with a 400. |

Register the options and pass them to the calls: `services.AddSingleton(appSettings.Pagination)`.

## Requests

Derive your query from the request type and add the filters. Declare it as a **class with `init` properties, not a positional record**, so `[AsParameters]` binds the inherited properties too.

```csharp
public class ListPostsQuery : PageRequest
{
    public string? Title { get; init; }
    public PostStatus? Status { get; init; }
}
```

| Type | Adds |
|---|---|
| `SearchRequest` | `orderBy`: repeated `field` or `field:asc` / `field:desc` (`orderBy=title:asc&orderBy=createdAt:desc`). |
| `PageRequest` | `pageNumber` (1-based, first page when left out) and `pageSize`. |
| `CursorRequest` | `cursor` (from the previous response; leave out for the first call) and `pageSize`. |
| `TimestampPageRequest` / `TimestampCursorRequest` | `createdFrom`, `createdTo`, `updatedFrom`, `updatedTo` (`ITimestampFilter`). |

Every range starts at its `From` (inclusive) and ends before its `To` (exclusive), so two ranges that touch never count the same item twice.

## The `SortMap`

A client may only order by what you allow. A `SortMap<T>` is declared once per entity:

```csharp
public static readonly SortMap<Post> Sorting = SortMap.For<Post>()
    .Allow(x => x.Title)                              // "title"
    .Allow(x => x.CreatedAt)                          // "createdAt"
    .Allow(x => x.Author.Handle, name: "author")      // a path needs a name
    .DefaultTo(x => x.CreatedAt, SortDirection.Desc);
```

| Method | Does |
|---|---|
| `Allow(x => x.Prop, name?)` | Allows ordering by the property. The client's name is the property name in camelCase (matching ignores case), or the name you give. A path through another object has no obvious name and needs one. Keep the expression translatable to SQL. |
| `DefaultTo(x => x.Prop, direction, name?)` | The ordering used when the request sends none. Also allows the field. |
| `DefaultTo("createdAt:desc")` | The same, written like the query; the name must already be allowed. |
| `ById(direction)` | Allows ordering by `id` and makes it the default. Lists with no default are ordered by id anyway. |
| `Fields` | The names a client may use. |

A name that is not declared is answered with a 400 that lists the allowed fields. **The id is always added as the last ordering**, so rows with equal values never change places between pages.

## Building the page

In memory (tests, small sources), synchronous:

```csharp
Result<Page<Post>> page = posts.AsQueryable().ToPage(request, options, Sorting);
Result<CursorPage<Post>> feed = posts.AsQueryable().ToCursorPage(request, options, Sorting);
```

With Entity Framework Core use the asynchronous versions, which share the same rules (`Aurore.Foundation.EntityFrameworkCore`):

```csharp
Result<Page<Post>> page = await db.Posts.Where(...).ToPageAsync(request, options, Sorting, cancellationToken);
Result<CursorPage<Post>> feed = await db.Posts.Where(...).ToCursorPageAsync(request, options, Sorting, cancellationToken);
```

Project to the response model **after** paging with `Map`: `page.Value.Map(PostResponse.From)`. Both page types have it.

Timestamp filters are applied before paging:

```csharp
var filtered = db.Posts.WhereTimestamps(request, created: x => x.CreatedAt, updated: x => x.UpdatedAt);   // Result<IQueryable<Post>>
```

## Errors

All invalid parameters of one request come back together as a single 400 with the offending parameters in `errors`:

| Code | When |
|---|---|
| `pagination.invalid_request` | `pageNumber` below 1 or too far out; `pageSize` outside 1..`MaxPageSize`; an `orderBy` that is malformed, not allowed or repeated; a `To` that is not after its `From`. |
| `pagination.invalid_cursor` | The cursor is not one this service wrote, or does not fit the ordering. |
| `pagination.cursor_mismatch` | The cursor came from a request with other filters or ordering. Start again without a cursor. |

## How the cursor works

The cursor carries the position (the values of the ordering fields of the last row) and a fingerprint of the filters and ordering, encoded. Nothing is stored on the server. The next page is a keyset query (`WHERE (a, b) > (...)`), so deep pages cost the same as the first. The page size may change between calls; the filters and ordering may not (that is the `cursor_mismatch`). The fingerprint is a guard against mistakes, not a security measure.

**Limits:** ordering a cursor list by a nullable property is not supported, because a comparison with a null never matches. Allowing such a field is a developer mistake and throws `InvalidOperationException`; order by a non-null property. A type with no `Id` can only be ordered by what its items say.

## Documenting the response

In the ASP.NET Core package, `.ProducesPage<T>()` and `.ProducesCursorPage<T>()` declare the 200 response of a paged endpoint in the OpenAPI document. The 400 is already declared for every endpoint. Query parameters are published in camelCase.
