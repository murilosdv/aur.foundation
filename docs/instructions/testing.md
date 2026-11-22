[Home](../../README.md) · [Documentation](../README.md)

# Testing

## What we test

Our own logic, never the framework or a package: a test that would pass with our code deleted is not worth keeping.

- Explicit `// Arrange`, `// Act`, `// Assert` sections (`// Arrange & Act` when they merge) and a `DisplayName` that reads as a rule.
- Scenario sets use `TheoryData` with `MemberData`, not long `InlineData` lists.
- Test folders mirror the source folders. `--filter-trait "Feature=Idempotency"` runs one feature.
- A helper used only inside one test file stays in it; a stub, sample or fixture that several tests use is its own file ([code style](./code-style.md)).

| Project | Holds |
|---|---|
| `Tests.Core`, `Tests.AspNetCore`, `Tests.Telemetry` | Unit tests. `Tests.AspNetCore` also runs the request pipeline and JWT authentication on an in-memory host. |
| `Tests.OpenApi`, `.Scalar`, `.Swagger` | Documents and UIs, each on an in-memory host. |
| `Tests.EntityFrameworkCore` | The tests every provider must pass (soft delete, timestamps, paging, conventions), written once as abstract classes, plus provider-independent ones. |
| `Tests.EntityFrameworkCore.Postgres` | Runs those tests on a real PostgreSQL, and tests what is specific to it. A new provider gets the whole set by adding a fixture and one small class per test. |
| `Tests.Shared` | Stubs and the in-memory host (`TestApp`). No tests. |

Real Keycloak tokens are tested in the [archetype](https://github.com/murilosdv/aur.archetypes.api), which has the host and identity provider they need.

## Database servers

Tests that need a server are marked `[Trait("Category", "Integration")]`, the others `Unit`. Run without servers with `--filter-not-trait "Category=Integration"`.

They use a real server because a fake only proves the fake works (SQL translation of sorting and keyset conditions, real unique violations). **The server must already be running:**

| Server | Address | Login |
|---|---|---|
| PostgreSQL | `127.0.0.1:5432` | `postgres` / `postgres` |

The machine's dev-sbx (`.dev-sbx`) provides it: `make sbx-up` starts it (`make sbx-down` removes its containers). Each test class creates its own database and drops it afterwards. `TEST_POSTGRES_HOST` changes the host.
Without a server the tests fail with a message that says so; they never skip, since a silent skip hides regressions.

Use `127.0.0.1`, not `localhost`: `localhost` resolves to `::1` first, and Docker in WSL does not forward IPv6.

In CI, the jobs of the two EF packages start their server with the [`start-services`](./shared-actions.md) action, using the same ports and passwords.

## Running

```
dotnet test --solution Foundation.slnx                                            # everything
dotnet test --project tests/Tests.Core/Tests.Core.csproj                          # one project
dotnet test --solution Foundation.slnx --filter-not-trait "Category=Integration"  # no servers needed
make test                                                                         # everything in a Linux container, with coverage
```

`make` also has `test-local` (runs `dotnet format` first, then the tests), `report` (opens the coverage report), `clean`, `sbx-up` / `sbx-down` for the dev-sbx and `sonar` (starts the dev-sbx with SonarQube, begin, build, tests with coverage, end, and removes SonarQube alone (the rest of the dev-sbx stays up); the token is `SONAR_TOKEN` in `.env`). Results go to `artifacts/test-results`, the coverage report to `artifacts/coverage-report/index.html`.
The container run matches CI (Linux, Release) and reaches the server on the `dev-sbx` network as `sbx-postgres`.

## Coverage

The reference is **80% line coverage per package**, measured by the container run without the test projects. It is not a gate: Windows-only code cannot count on Linux, and we do not write tests to move the number.
A package well below it is missing tests of real logic or carries code that should be excluded.

Exclude with `[ExcludeFromCodeCoverage]` what holds no logic:

- Records or classes that only hold settings or data (`*Options`).
- Methods that only chain framework calls to register something, with no decision, loop or computed value. Tag the method, not the class, when a sibling has logic.
- Attributes that only name a type.

Anything with a decision, computed value or error handling is tested, even wiring: a registration that depends on a setting is logic. Generated code is filtered out of the report (`-classfilters` in the `Dockerfile`).
