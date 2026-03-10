---
applyTo: "src/Test/**"
---

# Testing Conventions

1. Frameworks & Patterns

    - [ ] xUnit + FluentAssertions + Moq — no other test/assertion/mocking frameworks
    - [ ] Arrange-Act-Assert (AAA) pattern with `// Arrange`, `// Act`, `// Assert` comments
    - [ ] Test names: `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
    - [ ] Each test method contains one `// Act` step
    - [ ] Tests are deterministic — no random values, no `DateTime.Now`, no `Guid.NewGuid()`
    - [ ] Tests are isolated — no shared mutable state, no static mutable fields, no `IClassFixture<T>` mutation
          across tests
    - [ ] No reflection or access to private/internal members — assert on public return values, thrown exceptions,
          or mock interactions

2. Mocking

    - [ ] Mocks only for external dependencies (database, HTTP, file system)
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
