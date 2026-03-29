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
    - [ ] Tests are deterministic — no random values, no `Guid.NewGuid()` — exception:
          `DateTime.UtcNow` / `DateTimeOffset.UtcNow` are allowed for time-relative assertions (e.g., expiration
          windows, reset timestamps) using before/after bracketing
    - [ ] Tests are isolated — no shared mutable state, no static mutable fields, no `IClassFixture<T>` mutation
          across tests
    - [ ] `[Theory]` when multiple scenarios share the same logic with only different input values — do not duplicate
          `[Fact]` methods that differ only in arrange data
    - [ ] `[InlineData]` for simple compile-time constants — `[MemberData]` for complex objects or computed values
          that cannot be expressed as `[InlineData]` attributes
    - [ ] No reflection or access to private/internal members — assert on public return values, thrown exceptions,
          or mock interactions
    - [ ] No tests for trivial code (e.g., plain auto-properties, simple DTOs with no logic, compiler-guaranteed
          behavior) — covered indirectly through tests that exercise real behavior
    - [ ] Values shared between the constructor (or shared setup) and test assertions as `private const` or
          `private static readonly` fields — constant array data as `private static readonly` fields (C# has no
          `const` array; inlining allocates a new array each time) — all other values (single-method arrange/assert,
          constructor filler not verified by any assertion, assertion-only literals) stay inline
    - [ ] No logic in tests — no `if`, `else`, `switch`, `for`, `foreach`, `while`, or `try`/`catch` in test
          methods — tests are linear Arrange-Act-Assert sequences
    - [ ] Async test methods return `Task` — not `async void`
    - [ ] No `Thread.Sleep()` or `Task.Delay()` — use deterministic time abstractions or controlled waits
    - [ ] Per-test setup in constructor, cleanup via `IDisposable` — no static initializers or manual lifecycle
          management
    - [ ] Test classes and `[Fact]`/`[Theory]` methods are `public`

2. Mocking

    - [ ] Mocks only for external dependencies (e.g., database, HTTP, file system)
    - [ ] No mocks for domain logic — test real implementations
    - [ ] Mock application services when testing controllers
    - [ ] Mock adapters when testing application layer
    - [ ] No `Mock.Verify*()` on methods whose return value is already captured and asserted
    - [ ] Configure mocks with only the methods the test exercises — no blanket `Setup` for unused members

3. Organization

    - [ ] Tests in correct project matching production structure (e.g., `emc.camus.security.jwt.test`)
    - [ ] Test classes mirror production code structure (e.g., `Configurations/JwtSettingsTests.cs`)
    - [ ] Integration tests in separate test projects or `Integration/` subfolder — not mixed with unit tests
    - [ ] Shared test builders extracted to `Helpers/` folder — test doubles (fakes, stubs, custom handlers)
          extracted even when used by a single test class
    - [ ] Each adapter test project name matches its production counterpart with `.test` suffix (e.g.,
          `emc.camus.security.jwt.test` → `emc.camus.security.jwt`)
    - [ ] One test class per production class — file name matches with `Tests` suffix
          (e.g., `AuthService` → `AuthServiceTests`) — exception: classes annotated with
          `[ExcludeFromCodeCoverage]` do not require a corresponding test class

4. Assertions

    - [ ] Specific FluentAssertions methods (e.g., `.BeEquivalentTo()`, `.ContainSingle()`) — no `.BeTrue()`/`.BeFalse()`
          wrapping compound expressions
    - [ ] Exception messages: wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
    - [ ] Never assert on `exception.Data` — assert on message patterns instead
    - [ ] No commented-out assertions
