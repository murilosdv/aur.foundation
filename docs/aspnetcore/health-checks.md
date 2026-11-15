[Home](../../README.md) · [Documentation](../README.md) · [ASP.NET Core](./README.md)

# Health checks

Three endpoints for three audiences: a process supervisor, a load balancer, an operator.

```csharp
services.AddStandardHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.HttpDependencies);   // registers the checks
app.MapStandardHealthChecks(appSettings.AppInformation);                                                              // maps the endpoints
```

## Endpoints

| Endpoint | Caller | Says |
|---|---|---|
| `/health/live` | Anonymous | The **process is up**: the component name in slug case (`InvoiceApi` → `invoice-api`), the version and a timestamp. Never touches a dependency, so it is safe for frequent probes. Use it for container liveness. |
| `/health/ready` | Anonymous | Whether the service **can take traffic**: only the *critical* checks, as a status without details. For load balancers and orchestrators. |
| `/health/status` | Authenticated | **Every** dependency with status, description, duration and tags. It reveals dependency names, so it needs a caller. |

`Healthy` and `Degraded` answer `200`; `Unhealthy` answers `503`. A **non-critical** dependency is registered with a Degraded failure status, so its failure never makes the service Unhealthy; a **critical** one does.

Responses are cached for `HealthChecks:CacheSeconds`, so a burst of probes does not become a burst of calls to the dependencies.

```json
"HealthChecks": { "CacheSeconds": 5 }
```

| Key | Default | Meaning |
|---|---|---|
| `CacheSeconds` | 5 | How long readiness and status answers are cached. `0` turns the cache off. |

### Answers

`/health/live`: `{ "name": "invoice-api", "version": "1.4.2", "timestamp": "…" }`.

`/health/ready` and `/health/status`: `{ "status": "Healthy", "totalDuration": "…", "timestamp": "…", "dependencies": [ { "name": "postgres", "status": "Healthy", "description": null, "duration": "…", "tags": ["readiness", "type-database", "critical"] } ] }`. `dependencies` is left out of `/ready`.

## What is checked

| Check | Added by | Critical |
|---|---|---|
| Each HTTP dependency that has a `HealthCheck` section | `AddStandardHealthChecks` ([HTTP dependencies](./http-dependencies.md)) | Per its `Critical` flag |
| PostgreSQL (`SELECT 1`) | `services.AddPostgresHealthCheck(options)` ([Postgres](../entity-framework/postgres.md#health-check)) | Yes |
| SQL Server (`SELECT 1`) | `services.AddSqlServerHealthCheck(options)` ([SQL Server](../entity-framework/sqlserver.md#health-check)) | Yes |

Your own checks use the standard `IHealthCheck`; give them the tags below so the endpoints include them.

## Tags

`HealthCheckProperties.Tags`:

| Tag | Effect |
|---|---|
| `liveness` | Reserved name for liveness checks. `/health/live` is a fixed answer and does not run registered checks. |
| `readiness` | Included in `/health/status` (and in `/health/ready` when also critical). |
| `critical` | A failure makes the service Unhealthy. |
| `non-critical` | A failure only degrades the service. |
| `type-database`, `type-httpservice` | Describe the kind of dependency. |

## Probing a dependency

A dependency's check calls its `ReadinessEndpoint` (default `/health/ready`, the same convention) with an `AUR-HealthCheck-Client` header carrying this service's component name in slug case, and expects a success status. Any other status, a timeout (`TimeoutSeconds`, default 5) or a connection failure gives the check's failure status.

## In a container

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
```

Use `/health/ready` for the load balancer, so a pod whose database is down stops receiving traffic without being killed.
