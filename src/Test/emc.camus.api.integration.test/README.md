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

### Collection Fixtures

Tests that share the same factory variant are grouped into xUnit collection fixtures. This avoids spinning up
multiple containers and prevents `CryptoProviderFactory` static cache collisions between concurrent hosts.

| Collection | Factory | Purpose |
| ---------- | ------- | ------- |
| `PostgreSqlTestGroup` | `ApiPostgreSqlFactory` | Shares a single PostgreSQL container across all PostgreSQL test classes |
| `InMemoryTestGroup` | `ApiInMemoryFactory` | Shares a single in-memory host across all in-memory test classes |

### Database Reset Strategy

PostgreSQL tests use [Respawn](https://github.com/jbogard/Respawn) to wipe all rows in the `camus` schema
before each test, then `DatabaseSeeder` re-inserts reference data (roles, users, permissions). This guarantees
deterministic state without recreating the container or re-running migrations.

The `DatabaseSeeder` sets the PostgreSQL session variable `app.current_username` to `'Admin'` before inserting,
so database triggers correctly populate `created_by` and `updated_by` audit fields — matching the behavior of the
migration seed data.

### Stub Secrets

`StubSecretProvider` replaces the Dapr secret provider in tests. Each factory instance generates its own RSA key
pair to avoid `CryptoProviderFactory.Default` static cache collisions when multiple factories run in parallel.

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
├── ApiInfo/          Feature area: API info endpoint tests
├── Auth/             Feature area: authentication and token endpoint tests
├── Fixtures/         Factory variants and collection fixture definitions
├── Helpers/          Shared utilities (auth, seeding, assertion extensions)
└── *.csproj          Project file with test dependencies
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
Collection fixtures guarantee sequential execution within the same collection and share a single factory instance.

### Docker-related failures in CI

GitHub Actions `ubuntu-latest` runners include Docker pre-installed. If using self-hosted runners, verify Docker
is available with `docker info`.
