[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# JSON serialization

There is **one set of JSON rules** for the whole service, defined in `JsonSerializerConfiguration`, so an API response, a file and a cached value agree.

| Rule | Setting |
|---|---|
| Property names | camelCase (`pageSize`), read case-insensitively. |
| Enums | Strings (`"Published"`), not numbers. |
| Trailing commas | Allowed when reading. |
| Reference cycles | Ignored when writing. |
| Output | Compact (not indented). |
| Encoding | Unsafe-relaxed escaping: non-ASCII text is written as is. |

## Using it

```csharp
var json = order.Serialize();                         // string
var order = json.Deserialize<Order>();
var indented = order.Serialize(o => o.WriteIndented = true);   // adjust for one call

await stream.SerializeAsync(order, cancellationToken: ct);
var back = await stream.DeserializeAsync<Order>(cancellationToken: ct);

order.SerializeToFile("order.json");                  // indented by default
var fromFile = "order.json".DeserializeFromFile<Order>();       // throws when missing...
var maybe = "order.json".DeserializeFromFile<Order>(throwOnError: false);   // ...or null
```

| Member | Does |
|---|---|
| `JsonSerializerConfiguration.Default()` | A new `JsonSerializerOptions` with the rules. |
| `Configure(options)` | Applies the rules to an existing options object. |
| `Serialize` / `Deserialize` | String conversion. Each takes an optional action that adjusts the options for that call. |
| `SerializeAsync` / `DeserializeAsync` | Stream conversion. |
| `SerializeToFile` / `DeserializeFromFile` | File conversion; paths go through `NormalizeRuntimePath`, so Windows drive paths work under WSL. |

## In the API

`builder.ConfigureStandardSerializers()` (ASP.NET Core package) makes the minimal API's `JsonOptions` use `Configure`, so request and response bodies follow the same rules.

Query parameter names are published in camelCase in the OpenAPI document by `AddStandardCamelCaseQueryParameters()`; binding ignores case, so only the document changes.
