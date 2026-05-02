# Integration Test Project

> **Parent Documentation:** [Test README](../README.md) | [Main README](../../../README.md)

End-to-end integration tests for the Camus API. Tests exercise the full HTTP pipeline — controllers, middleware,
authentication, persistence, and database triggers — using real infrastructure managed by Testcontainers.

---

## Configuration

### Factory Variants

Each factory variant boots a complete `WebApplicationFactory<Program>` host configured for a specific persistence
backend. Factories live in `Fixtures/` and share a common base class (`ApiFactoryBase`) for cross-cutting
concerns like stub secrets, rate-limit overrides, and xUnit log routing.

| Factory | Backend | Infrastructure |
| ------- | ------- | -------------- |
| `ApiPostgreSqlFactory` | PostgreSQL (Testcontainers) | Real container, DBUp migrations, Respawn reset |
| `ApiInMemoryFactory` | In-memory | No external dependencies |
| `ApiRateLimitingFactory` | In-memory | Tight rate limits for IP-partition testing |
| `ApiTimeoutFactory` | In-memory | Short request timeouts for timeout testing |

### Collection Fixtures

Tests that share the same factory variant are grouped into xUnit collection fixtures. This avoids spinning up
multiple containers and prevents `CryptoProviderFactory` static cache collisions between concurrent hosts.

| Collection Name | Fixture Class | Factory | Purpose |
| --------------- | ------------- | ------- | ------- |
| `PostgreSQL` | `PostgreSqlTestGroup` | `ApiPostgreSqlFactory` | Shares a single PostgreSQL container across all PostgreSQL test classes |
| `InMemory` | `InMemoryTestGroup` | `ApiInMemoryFactory` | Shares a single in-memory host across all in-memory test classes |
| `RateLimiting` | `RateLimitingTestGroup` | `ApiRateLimitingFactory` | Shares a single host with tight rate limits for IP-partition tests |
| `Timeout` | `TimeoutTestGroup` | `ApiTimeoutFactory` | Shares a single host with short timeouts for timeout tests |

> **Parallel collections are disabled** via `xunit.runner.json` (`parallelizeTestCollections: false`).
> `WebApplicationFactory<Program>` with the minimal hosting model uses a global `DiagnosticSource` listener
> to intercept `WebApplication.CreateBuilder()`. When xUnit runs collections in parallel, multiple factories
> subscribe their listeners concurrently, causing `UseSetting` configuration values from one factory to leak
> into another factory's host. This produces flaky failures where rate-limit or timeout settings from one
> variant are applied to a different variant's host. Sequential collection execution eliminates the cross-wiring.

### Database Reset Strategy

PostgreSQL tests use [Respawn](https://github.com/jbogard/Respawn) to wipe all rows in the `camus` schema
before each test, then `DatabaseSeeder` re-inserts reference data (api_info, roles, permissions, users). This guarantees
deterministic state without recreating the container or re-running migrations.

The `DatabaseSeeder` sets the PostgreSQL session variable `app.current_username` to `'Admin'` before inserting,
so database triggers correctly populate `created_by` and `updated_by` audit fields — matching the behavior of the
migration seed data.

### Stub Secrets

`StubSecretProvider` replaces the Dapr secret provider in tests. Each factory instance generates its own RSA key
pair to avoid `CryptoProviderFactory.Default` static cache collisions when multiple factory instances coexist in
the same process.

### Test Frameworks

| Package | Purpose |
| ------- | ------- |
| xUnit | Test framework and runner |
| FluentAssertions | Assertion library with expressive syntax |
| Testcontainers.PostgreSql | Manages PostgreSQL container lifecycle |
| Respawn | Resets database state between tests |
| Dapper | Direct database assertions (transitive dependency) |
| MartinCostello.Logging.XUnit.v3 | Routes application logs to xUnit test output |

---

## Integration

### Prerequisites

Docker must be running. Testcontainers automatically pulls `postgres:16-alpine` on first run.

For run commands and VS Code tasks, see the [Test README](../README.md#integration).

### Project Structure

```text
emc.camus.api.integration.test/
├── ApiInfo/              Feature area: API info endpoint tests
├── Auth/                 Feature area: authentication and token endpoint tests
├── Common/               Middleware, rate-limiting, telemetry, and timeout tests
├── Fixtures/             Factory variants and collection fixture definitions
├── HealthChecks/         Health check endpoint tests
├── Helpers/              Shared utilities (auth, seeding, assertion extensions)
├── InMemoryCache/        Token revocation cache tests
├── PostgreSqlPersistence/ Unit-of-work transaction tests
├── xunit.runner.json     Runner configuration
└── *.csproj              Project file with test dependencies
```

---

## Troubleshooting

### Tests fail with "No tables found"

Respawn runs before DBUp migrations have executed. Ensure the factory's `InitializeAsync` forces host creation
(`_ = Server`) before any test calls `ResetDatabaseAsync`.

### Tests fail with `ObjectDisposedException` on RSA key

Multiple factory instances share the same RSA key material, causing `CryptoProviderFactory.Default` cache
collisions. Ensure `StubSecretProvider` generates per-instance keys (not static).

### Tests pass individually but fail when run together

Test classes targeting the same infrastructure must use `[Collection(...)]` instead of `IClassFixture<T>`.
Collection fixtures guarantee sequential execution within the same collection and share a single factory
instance. Additionally, parallel collection execution must remain disabled in `xunit.runner.json` — see
[Collection Fixtures](#collection-fixtures) for details on the `DiagnosticSource` cross-wiring issue.

### Docker-related failures in CI

GitHub Actions `ubuntu-latest` runners include Docker pre-installed. If using self-hosted runners, verify Docker
is available with `docker info`.
