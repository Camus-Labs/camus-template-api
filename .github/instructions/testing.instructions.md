---
applyTo: "src/Test/**"
---

# Testing Conventions

1. Test Patterns

    - [ ] xUnit + FluentAssertions — no other test or assertion frameworks
    - [ ] Arrange-Act-Assert (AAA) pattern with `// Arrange`, `// Act`, `// Assert` comments
    - [ ] Test names: `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
    - [ ] Each test method contains one `// Act` step — multiple assertions on the same act result belong in one
          test, not split into separate methods
    - [ ] Tests are deterministic — no random values, no `Guid.NewGuid()` — exception:
          `DateTime.UtcNow` / `DateTimeOffset.UtcNow` are allowed for time-relative assertions (e.g., expiration
          windows, reset timestamps) using before/after bracketing
    - [ ] `[Theory]` when multiple scenarios share the same logic with only different input values — do not duplicate
          `[Fact]` methods that differ only in arrange data
    - [ ] `[InlineData]` for simple compile-time constants — `[MemberData]` for complex objects or computed values
          that cannot be expressed as `[InlineData]` attributes
    - [ ] No logic in tests — no `if`, `else`, `switch`, `for`, `foreach`, `while`, or `try`/`catch` in test
          methods — tests are linear Arrange-Act-Assert sequences
    - [ ] Async test methods return `Task` — not `async void`
    - [ ] Test classes and `[Fact]`/`[Theory]` methods are `public`

2. Organization

    - [ ] Unit test projects use `.test` suffix — integration test projects use `.integration.test` suffix
    - [ ] Shared test builders extracted to `Helpers/` folder — test doubles (fakes, stubs, custom handlers)
          extracted even when used by a single test class

3. Assertions

    - [ ] Specific FluentAssertions methods (e.g., `.BeEquivalentTo()`, `.ContainSingle()`) — no `.BeTrue()`/
          `.BeFalse()` wrapping compound expressions
    - [ ] No commented-out assertions
