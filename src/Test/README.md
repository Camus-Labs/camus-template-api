# Test Projects

> **Parent Documentation:** [Main README](../../README.md) | [Contributing](../../CONTRIBUTING.md)

Unit test suite for the Camus API. One test project per production layer/adapter, all sharing
common frameworks and conventions.

---

## Configuration

### Shared Build Properties

`Directory.Build.props` in this folder applies to all test projects. It inherits from the root
`src/Directory.Build.props` and adds test-specific settings (disables documentation generation,
marks projects as non-packable).

### Coverage Settings

`coverlet.runsettings` at the repository root configures code coverage collection. It excludes
test assemblies and auto-generated code from coverage metrics, and outputs Cobertura XML format
for report generation.

### Test Frameworks

All test projects use the same stack — defined in each `.csproj`:

| Package | Purpose |
| ------- | ------- |
| xUnit | Test framework and runner |
| FluentAssertions | Assertion library with expressive syntax |
| Moq | Mocking framework for dependency isolation |
| Coverlet | Code coverage collection |

### Project Mapping

Each test project mirrors a production project with a `.test` suffix:

| Test Project | Production Project |
| ------------ | ------------------ |
| `emc.camus.api.test` | `Api/emc.camus.api` |
| `emc.camus.application.test` | `Application/emc.camus.application` |
| `emc.camus.domain.test` | `Domain/emc.camus.domain` |
| `emc.camus.cache.inmemory.test` | `Adapters/emc.camus.cache.inmemory` |
| `emc.camus.documentation.swagger.test` | `Adapters/emc.camus.documentation.swagger` |
| `emc.camus.observability.otel.test` | `Adapters/emc.camus.observability.otel` |
| `emc.camus.persistence.postgresql.test` | `Adapters/emc.camus.persistence.postgresql` |
| `emc.camus.ratelimiting.inmemory.test` | `Adapters/emc.camus.ratelimiting.inmemory` |
| `emc.camus.secrets.dapr.test` | `Adapters/emc.camus.secrets.dapr` |
| `emc.camus.security.apikey.test` | `Adapters/emc.camus.security.apikey` |
| `emc.camus.security.jwt.test` | `Adapters/emc.camus.security.jwt` |

---

## Integration

### Running Tests

Run all tests from the repository root:

```text
dotnet test src/CamusApp.sln
```

Run a single test project:

```text
dotnet test src/Test/emc.camus.domain.test
```

### Coverage Reports

Generate a coverage report using the VS Code tasks:

1. Run the **test-with-coverage** task — executes all tests with Coverlet collection
2. Run the **test-coverage-report** task — generates an HTML report and opens it in the browser

Coverage output lands in `src/Test/*/TestResults/` (git-ignored). The HTML report is generated
in `coveragereport/` at the repository root.

### Conventions Summary

Tests follow the conventions defined in `.github/instructions/testing.instructions.md`.

---

## Troubleshooting

### Tests fail with missing project references

Each test `.csproj` must reference its production project. Verify the `<ProjectReference>` element
points to the correct relative path.

### Coverage report shows 0% or is missing

Ensure the **test-with-coverage** task runs before generating the report. The task cleans previous
`TestResults/` directories, rebuilds, and collects fresh coverage data.

### Tests pass locally but fail in CI

Check for environment-dependent code: file paths, time zones, or locale-specific formatting.
Tests must be deterministic with no dependency on machine state.
