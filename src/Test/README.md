# Test Projects

> **Parent Documentation:** [Main README](../../README.md) | [Contributing](../../CONTRIBUTING.md)

Test suite for the Camus API. Unit test projects (`.test` suffix) cover individual layers and adapters.
The integration test project (`.integration.test` suffix) exercises the full HTTP pipeline with real infrastructure.

---

## Configuration

### Shared Build Properties

`Directory.Build.props` in this folder applies to all test projects.

It inherits from the root `src/Directory.Build.props` and adds test-specific settings (disables documentation
generation, marks projects as non-packable).

### Coverage Settings

`coverlet.runsettings` at the repository root configures code coverage collection. It excludes test assemblies
and auto-generated code from coverage metrics, and outputs Cobertura XML format for report generation.

### Unit Test Frameworks

| Package | Purpose |
| ------- | ------- |
| xUnit | Test framework and runner |
| FluentAssertions | Assertion library with expressive syntax |
| Moq | Mocking framework for dependency isolation |
| Coverlet | Code coverage collection |

### Project Mapping

#### Unit Test Projects

Each unit test project mirrors a production project with a `.test` suffix:

| Test Project | Production Project |
| ------------ | ------------------ |
| `emc.camus.api.test` | `Api/emc.camus.api` |
| `emc.camus.application.test` | `Application/emc.camus.application` |
| `emc.camus.domain.test` | `Domain/emc.camus.domain` |
| `emc.camus.cache.inmemory.test` | `Adapters/emc.camus.cache.inmemory` |
| `emc.camus.documentation.swagger.test` | `Adapters/emc.camus.documentation.swagger` |
| `emc.camus.observability.otel.test` | `Adapters/emc.camus.observability.otel` |
| `emc.camus.persistence.inmemory.test` | `Adapters/emc.camus.persistence.inmemory` |
| `emc.camus.persistence.postgresql.test` | `Adapters/emc.camus.persistence.postgresql` |
| `emc.camus.ratelimiting.inmemory.test` | `Adapters/emc.camus.ratelimiting.inmemory` |
| `emc.camus.secrets.dapr.test` | `Adapters/emc.camus.secrets.dapr` |
| `emc.camus.security.apikey.test` | `Adapters/emc.camus.security.apikey` |
| `emc.camus.security.jwt.test` | `Adapters/emc.camus.security.jwt` |

#### Integration Test Project

| Test Project | Scope |
| ------------ | ----- |
| [`emc.camus.api.integration.test`](emc.camus.api.integration.test/README.md) | End-to-end API tests with Testcontainers (PostgreSQL) and in-memory variants |

See the [integration test README](emc.camus.api.integration.test/README.md) for architecture decisions, factory
variants, database reset strategy, and integration-specific troubleshooting.

---

## Integration

### Running Tests

Solution filters scope test runs to the correct projects:

```text
# Unit tests only
dotnet test src/UnitTests.slnf

# Integration tests only (requires Docker)
dotnet test src/IntegrationTests.slnf

# All tests
dotnet test src/CamusApp.sln
```

Or use the VS Code tasks: **test-unit**, **test-integration**, **test-all**.

### Coverage Reports

Generate a coverage report using the VS Code tasks:

1. Run the **test-with-coverage** task — executes unit tests with Coverlet collection
2. Run the **test-coverage-report** task — generates an HTML report and opens it in the browser

Coverage output lands in `src/Test/*/TestResults/` (git-ignored). The HTML report is generated in `coveragereport/`
at the repository root.

### Conventions

- Unit tests follow `.github/instructions/testing.unit.instructions.md`
- Integration tests follow `.github/instructions/testing.integration.instructions.md`

---

## Troubleshooting

### Tests fail with missing project references

Each test `.csproj` must reference its production project. Verify the `<ProjectReference>` element points to
the correct relative path.

### Coverage report shows 0% or is missing

Ensure the **test-with-coverage** task runs before generating the report. The task cleans previous `TestResults/`
directories, rebuilds, and collects fresh coverage data.

### Tests pass locally but fail in CI

Check for environment-dependent code: file paths, time zones, or locale-specific formatting. Tests must be
deterministic with no dependency on machine state.

### Integration test issues

See the [integration test README troubleshooting section](emc.camus.api.integration.test/README.md#troubleshooting)
for Docker, Respawn, and fixture-specific issues.
