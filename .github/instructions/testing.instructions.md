---
applyTo: "src/Test/**"
---

# Testing Conventions

1. Quality

    - [ ] xUnit + FluentAssertions + Moq — no other test/assertion/mocking frameworks
    - [ ] Arrange-Act-Assert (AAA) pattern with `// Arrange`, `// Act`, `// Assert` comments
    - [ ] Test names: `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
    - [ ] Each test validates one specific behavior
    - [ ] Tests are isolated and deterministic (no random values, no `DateTime.Now`)
    - [ ] No tests checking implementation details — test behavior, not internals

2. Mocking

    - [ ] Mocks only for external dependencies (database, HTTP, file system)
    - [ ] Domain logic NOT mocked — test real implementations
    - [ ] Application services mocked when testing controllers
    - [ ] Adapters mocked when testing application layer
    - [ ] Mock setup minimal — verify interactions only when behavior matters

3. Organization

    - [ ] Tests in correct project matching production structure (e.g., `emc.camus.security.jwt.test`)
    - [ ] Test classes mirror production code structure (e.g., `Configurations/JwtSettingsTests.cs`)
    - [ ] Integration tests separated from unit tests
    - [ ] Test fixtures/helpers reused — no duplication
    - [ ] Each adapter has its own test project

4. Assertions

    - [ ] FluentAssertions for all assertions — no raw `Assert.*`
    - [ ] Specific assertions (no `Assert.True` for complex conditions)
    - [ ] Exception messages: wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
    - [ ] Never assert on `exception.Data` — assert on message patterns instead
    - [ ] No commented-out assertions
