[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Constants

Shared names and sizes. `FieldLengths`, `Patterns`, `PlatformNames`, `FeatureOrder` and `StandardErrors` are `abstract` **on purpose: inherit from them** to add your own, so yours and the foundation's are reached from the same class.

## `FieldLengths`

Text sizes in multiples of 8, **named by size, not by what they hold**:

| Constant | Value |
|---|---|
| `Tiny` | 16 |
| `Small` | 32 |
| `Medium` | 64 |
| `Large` | 128 |
| `XLarge` | 256 |
| `XXLarge` | 512 |
| `Text` | 2048 |
| `LongText` | 32768 |

An application gives them meaning in its own rules and uses that **one value in both the validator and the column**, so the two cannot drift:

```csharp
public sealed class BlogRules : FieldLengths
{
    public const int TitleMaxLength = Large;
    public const int HandleMaxLength = Small;
}

RuleFor(x => x.Title).MaximumLength(BlogRules.TitleMaxLength);       // validator
builder.Property(x => x.Title).HasMaxLength(BlogRules.TitleMaxLength);   // column
```

## `Patterns`

Regular expressions for validators. Currently `Patterns.Slug` (`^[a-z0-9]+(-[a-z0-9]+)*$`: lower case words joined by single hyphens). Inherit to add more.

## `PlatformNames`

The names the foundation gives to what it adds to telemetry and requests, built from **one prefix** (`aur` by default). They are the contract with whoever reads your telemetry.

| Name | Value with the default prefix |
|---|---|
| `PlatformNames.Telemetry.ServiceDomain` | `aur.service.domain` |
| `PlatformNames.Telemetry.ServiceBoundary` | `aur.service.boundary` |
| `PlatformNames.Telemetry.ServiceMaintainer` | `aur.service.maintainer` |
| `PlatformNames.Telemetry.CorrelationId` | `aur.correlation.id` |
| `PlatformNames.Telemetry.FeatureName` | `aur.feature.name` |
| `PlatformNames.Telemetry.ErrorCode` | `aur.error.code` |
| `PlatformNames.HealthChecks.ClientHeader` | `AUR-HealthCheck-Client` |

To use another prefix (for a platform of your own), change it **once, as the first line of `Program.cs`**, before anything is built:

```csharp
PlatformNames.UsePrefix("acme");     // lower case letters and digits, starting with a letter; otherwise ArgumentException
```

`Aurore` itself appears only in namespaces and package names; on the wire the mark is `aur.` / `AUR-`.

## `OpenTelemetryProperties`

The names of OpenTelemetry semantic-convention attributes the foundation uses (`service.name`, `service.version`, `service.instance.id`, `deployment.environment.name`, `http.request.method`, `server.address`, `url.path`, `url.query`, `client.address`, `network.peer.address`, `enduser.id`, `user_agent.original`), so spans, logs and metrics agree on spelling.

## `TimestampFormats`

Format strings for log timestamps: `TimeOnly` (`HH:mm:ss`), `TimeOnlyWithTrailingSpace`, and `Iso8601.DateOnly`, `Iso8601.DateTimeWithoutOffset` and `Iso8601.UtcWithMilliseconds` (`yyyy-MM-ddTHH:mm:ss.fffZ`).

## Constants in other packages

| Where | Constants |
|---|---|
| Core | `ExitCodes`, `HealthCheckProperties` (endpoint paths, tags, defaults), `IdentityProviderPaths` |
| AspNetCore | `CustomHeaderNames`, `ApiKeyDefaults`, `AuthenticationDefaults`, `KeycloakClaims` |
| OpenApi | `SecuritySchemes` |
| EntityFrameworkCore | `DatabaseDefaults.Schema` (`app`), `QueryFilters.SoftDelete` |
| Postgres | `PostgresSqlStates` (inheritable) |
