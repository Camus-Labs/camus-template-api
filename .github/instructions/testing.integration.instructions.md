---
applyTo: "src/Test/**integration.test/**"
---

# Integration Testing Conventions

1. Frameworks & Infrastructure

    - [ ] Testcontainers for database and infrastructure dependencies (e.g., `PostgreSqlContainer` for PostgreSQL)
    - [ ] `WebApplicationFactory<Program>` for API integration tests — tests the full HTTP pipeline in-process
    - [ ] `[Trait("Category", "Integration")]` on every test class — enables CI filtering via
          `dotnet test --filter "Category=Integration"`
    - [ ] No mocks for infrastructure — tests use real database connections, real HTTP clients, real middleware
    - [ ] `MartinCostello.Logging.XUnit` for routing application logs to xUnit test output — enables
          debugging test failures in IDE and CI

2. Fixture Management

    - [ ] Shared infrastructure in `Fixtures/` folder
    - [ ] One factory per configuration variant under test (e.g., a specific persistence provider,
          authentication scheme, or infrastructure setup) — no hybrid configurations
    - [ ] All factories extend a shared base factory class that handles common concerns — variant factories
          override only what differs
    - [ ] Test constructors set `factory.OutputHelper = outputHelper` to route application logs to the
          current test's output
    - [ ] `[CollectionDefinition]` + `ICollectionFixture<T>` to share a factory across multiple test classes
          that need the same infrastructure — one collection per factory variant (e.g., `PostgreSqlTestGroup`,
          `InMemoryTestGroup`)
    - [ ] `[CollectionDefinition]` + `ICollectionFixture<T>` instead of `IClassFixture<T>` — prevents parallel
          `WebApplicationFactory<Program>` host interference from `CryptoProviderFactory` static caches and
          other shared static state
    - [ ] No container creation inside individual test methods — always use fixture-managed factories
    - [ ] Authenticated requests use shared helper methods in `Helpers/` — no hardcoded tokens,
          API keys, or credentials in test methods
    - [ ] Database tests implement `IAsyncLifetime` and call `ResetDatabaseAsync()` in `InitializeAsync` —
          Respawn deletes all data in the schema, then `DatabaseSeeder` re-inserts reference data
    - [ ] Factories that require external infrastructure manage the lifecycle via `IAsyncLifetime` and force
          host creation in `InitializeAsync` (e.g., `_ = Server`) so migrations run before tests execute
    - [ ] Factories backed by in-memory or stub implementations require no external dependencies
    - [ ] `StubSecretProvider` generates per-instance RSA keys — never use static key material that would
          collide in `CryptoProviderFactory.Default` across concurrent factory hosts

3. Scope & Coverage

    - [ ] One happy path + one key failure path per feature boundary — not exhaustive input permutation testing
    - [ ] Every test class exercises at least two layers (e.g., API → Application, Application → Persistence) —
          single-layer logic belongs in unit tests
    - [ ] No `Assert.Equal` or FluentAssertions checks on pure in-memory computation results that involve zero
          infrastructure calls — those assertions belong in unit tests
    - [ ] All integration tests go through the HTTP pipeline via `HttpClient` — no direct service-class
          invocation

4. Organization

    - [ ] Test classes organized by feature area — not mirroring production class structure one-to-one
    - [ ] Fixture classes (factories, collection definitions) in `Fixtures/` folder
    - [ ] Test settings files in `Settings/` folder

5. Assertions

    - [ ] HTTP status code assertions use `await response.Should().HaveStatusCode(expected)` — the
          `HaveStatusCode` extension (in `Helpers/HttpResponseAssertionExtensions.cs`) reads and includes the
          response body in the failure message for immediate diagnostics
    - [ ] Never use `response.StatusCode.Should().Be(...)` — it omits the response body on failure
    - [ ] HTTP response assertions verify status code and response body structure
    - [ ] Failure status code assertions use `await response.Should().HaveErrorCode(expected)` — the
          `HaveErrorCode` extension (in `Helpers/HttpResponseAssertionExtensions.cs`) deserializes the
          ProblemDetails body and asserts the `error` property — not just the HTTP status code alone
    - [ ] Database assertions query the database directly after the act step — not through the system under test
    - [ ] Database assertions use `NpgsqlConnection` + Dapper with the factory's `ConnectionString` property
    - [ ] Audit trail assertions verify `action_audit` records alongside business data assertions
