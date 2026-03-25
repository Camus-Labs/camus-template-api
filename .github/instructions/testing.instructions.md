---
applyTo: "src/Test/**"
---

# Testing Conventions

1. Frameworks & Patterns

    - [ ] xUnit + FluentAssertions + Moq — no other test/assertion/mocking frameworks
    - [ ] Arrange-Act-Assert (AAA) pattern with `// Arrange`, `// Act`, `// Assert` comments
    - [ ] Test names: `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
    - [ ] Each test method contains one `// Act` step — multiple assertions on the same act result belong in one
          test, not split into separate methods
    - [ ] Tests are deterministic — no random values, no `DateTime.Now`, no `Guid.NewGuid()`
    - [ ] Tests are isolated — no shared mutable state, no static mutable fields, no `IClassFixture<T>` mutation
          across tests
    - [ ] No reflection or access to private/internal members — assert on public return values, thrown exceptions,
          or mock interactions
    - [ ] No tests for trivial code (e.g., plain auto-properties, simple DTOs with no logic, compiler-guaranteed
          behavior) — covered indirectly through tests that exercise real behavior
    - [ ] Reusable test values as `private const` or `private static readonly` fields — assertions reference these
          constants instead of inline literals to keep a single source of truth

2. Mocking

    - [ ] Mocks only for external dependencies (e.g., database, HTTP, file system)
    - [ ] Domain logic NOT mocked — test real implementations
    - [ ] Application services mocked when testing controllers
    - [ ] Adapters mocked when testing application layer
    - [ ] No `Mock.Verify*()` on methods whose return value is already captured and asserted

3. Organization

    - [ ] Tests in correct project matching production structure (e.g., `emc.camus.security.jwt.test`)
    - [ ] Test classes mirror production code structure (e.g., `Configurations/JwtSettingsTests.cs`)
    - [ ] Integration tests in separate test projects or `Integration/` subfolder — not mixed with unit tests
    - [ ] Shared test builders and fixtures extracted to `Helpers/` or `Fixtures/` folder
    - [ ] Each adapter test project name matches its production counterpart with `.test` suffix (e.g.,
          `emc.camus.security.jwt.test` → `emc.camus.security.jwt`)

4. Assertions

    - [ ] Specific FluentAssertions methods (e.g., `.BeEquivalentTo()`, `.ContainSingle()`) — no `.BeTrue()`/`.BeFalse()`
          wrapping compound expressions
    - [ ] Exception messages: wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
    - [ ] Never assert on `exception.Data` — assert on message patterns instead
    - [ ] No commented-out assertions
