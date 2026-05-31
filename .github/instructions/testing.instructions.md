---
applyTo: "src/Test/**"
---

# Testing Conventions

1. Test Patterns

    - [ ] xUnit + FluentAssertions — no other test or assertion frameworks
    - [ ] Arrange-Act-Assert (AAA) pattern with `// Arrange`, `// Act`, `// Assert` comments —
          `// Arrange` may be omitted when the test has no arrange statements beyond constructor setup
    - [ ] Test names follow `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
    - [ ] Each test method contains one `// Act` step — multiple assertions on the same act result belong in one
          test, not split into separate methods
    - [ ] Tests are deterministic — no random values, no `Guid.NewGuid()` — exception: cryptographic
          key generation (e.g., `RSA.Create()`) is permitted in `static readonly` fixtures when tests
          assert on structure, not key material
    - [ ] Tests with multiple scenarios sharing the same logic use `[Theory]`
    - [ ] Duplicate `[Fact]` methods that differ only in arrange data are not permitted — exception: keep separate
          `[Fact]` methods when each test verifies a distinct contract or obligation rather than the same behavior
          with varying input data (e.g., per-parameter null-guard tests are distinct obligations — each protects
          a different dependency and maps to a separate guard clause)
    - [ ] `[MemberData]` never supplies delegate parameters (`Action`, `Func`, lambdas) — delegates collapse
          the Arrange step into the data provider and break AAA visibility
    - [ ] Tests use `[InlineData]` when every argument is an attribute-compatible literal (primitives, strings,
          enums, `typeof`)
    - [ ] Tests use `[MemberData]` when any argument requires object construction or runtime computation
    - [ ] No logic in tests — no `if`, `else`, `switch`, `for`, `foreach`, `while`, or `try`/`catch` in test
          methods
    - [ ] Async test methods return `Task` — not `async void`
    - [ ] All instance field initialization in constructor body — no inline field initializers
    - [ ] Structural literals (e.g., endpoint URLs, header names, query strings, route patterns) that appear more
          than once in the class are declared as `private const` fields — these are infrastructure plumbing
          with high churn risk
    - [ ] Specification literals (e.g., expected assertion values like `"Healthy"`, `"nosniff"`, `"DENY"`,
          status descriptions, error messages) stay inline even when repeated — they document the expected
          behavior at the point of verification and aid test readability
    - [ ] Arrays and collections always declared as `private static readonly` fields regardless of content category
          — exception: `TheoryData` fields used as `[MemberData]` sources remain `public static readonly`
          because xUnit requires public visibility for test data discovery
    - [ ] Single-occurrence literals of any category stay inline — exception: arrays always use
          `private static readonly` fields regardless of usage count

2. Organization

    - [ ] Unit test projects use `.test` suffix
    - [ ] Integration test projects use `.integration.test` suffix
    - [ ] Shared test builders reside in `Helpers/` folder
    - [ ] Test doubles (fakes, stubs, custom handlers) reside in dedicated classes — even when used by a single
          test class

3. Assertions

    - [ ] Specific FluentAssertions methods (e.g., `.BeEquivalentTo()`, `.ContainSingle()`) — no `.BeTrue()`/
          `.BeFalse()` wrapping compound expressions
    - [ ] No commented-out assertions
    - [ ] Exception messages use wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
    - [ ] Never assert on `exception.Data` — assert on message patterns instead

4. Test Boundaries

    - [ ] No reflection or access to private/internal members — assert on public return values, thrown exceptions,
          or mock interactions
