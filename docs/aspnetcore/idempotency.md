[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Idempotency

A client that does not know whether its `POST` arrived will retry it, and the order is created twice. With idempotency the client sends an **`Idempotency-Key`** and the service replays the first successful answer instead of doing the work again.

## Opting in

```csharp
builder.MapPost("", Handle).WithIdempotency();                   // optional key
builder.MapPost("", Handle).WithIdempotency(required: true);     // 400 without the header
```

Opt in per endpoint, on creating (non-idempotent by nature) operations. `PUT` and `DELETE` are already idempotent by their meaning.

## The key

12 to 128 characters of letters, digits, `.`, `_` or `-` (a UUID works). An invalid key is a `400`. The OpenAPI document lists the header on exactly the operations that use it, required where `required: true` ([OpenAPI](../openapi/README.md)).

## What happens

1. First request with a key: the endpoint runs. If it answers with a **2xx**, the answer is stored.
2. Same caller repeating the same key on the same method and path: the endpoint **does not run**. The stored answer comes back with the header **`Idempotency-Replayed: true`**.

A replay carries the stored status, the JSON body, the headers the endpoint itself added and, for `Created`/`Accepted` results, the `Location`. Per-request headers (`Correlation-Id`, `Date`, …) are those of the new request.

## The rules

- **The key means the request.** It is scoped to the caller (user id, or client IP when anonymous), the method and the path. The same key always means the same request **whatever the body says**: sending a different body with a used key still gets the first answer. Two callers using the same key never see each other's result.
- **Only successful (2xx) results are stored.** A failure is not cached, so the client can retry and succeed.
- Only results the filter can write again are stored: typed results with a status and plain objects (written as 200 JSON). A plain `string` result is written as text and is not stored.
- Stored for `RequestPipeline:Idempotency:CacheDurationSeconds` (24 hours by default).

## Where it is stored

The store is `HybridCache`: **in memory** by default, so it is best effort, per instance, and lost on restart. Register an `IDistributedCache` (for example `services.AddStackExchangeRedisCache(...)`) and HybridCache uses it as its second level; nothing else changes and the replays then survive restarts and work across instances.

## What it is not

It is a convenience for clients that retry, **not** a uniqueness guarantee. Guarantee uniqueness in the database too (a unique index) and answer the violation with a conflict ([unique violations](../entity-framework/unique-violations.md)).
