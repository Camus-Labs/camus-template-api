---
applyTo: "{src/Test/**/*.cs,!src/Test/**integration.test/**/*.cs}"
---

# Unit Testing Conventions

1. Frameworks & Mocking

    - [ ] Moq as the only mocking framework — no other mocking libraries
    - [ ] Mocks only for abstractions (interfaces or abstract classes) that cross process or I/O boundaries
          (e.g., database, HTTP, file system, clock, hosting environment)
    - [ ] No mocks for domain logic — test real implementations
    - [ ] Controller tests mock application services — not real implementations
    - [ ] Application-layer tests mock adapters — not real infrastructure
    - [ ] Repository tests mock DataAccess interfaces — not real data stores
    - [ ] No `Mock.Verify*()` on methods whose return value is already captured and asserted
    - [ ] Configure mocks with only the methods the class exercises — no `Setup` for members that no test
          in the class uses

2. Isolation & Setup

    - [ ] Tests are isolated — no shared mutable state across tests — exception: `private static readonly`
          fields (cryptographic fixtures, collections, test data) are permitted when no test or SUT mutates them
    - [ ] Cleanup via `IDisposable` — no static initializers or manual lifecycle management
    - [ ] No `Thread.Sleep()` or `Task.Delay()` — use deterministic time abstractions or controlled waits
    - [ ] Inject `FakeTimeProvider` (from `Microsoft.Extensions.Time.Testing`) when the SUT depends on
          `TimeProvider`
    - [ ] Derive all time-sensitive assertions from `_timeProvider.GetUtcNow()` — no inline
          `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in assertions
    - [ ] When the SUT does not depend on `TimeProvider`, dates used purely as test input/output data use a
          `private static readonly` field (e.g., `private static readonly DateTimeOffset FixedNow = ...`) with
          derived values expressed relative to it — no inline `new DateTime(...)` literals scattered across methods

3. Scope & Coverage

    - [ ] No mock overrides to force execution into a branch unreachable via the SUT's public API — tests exercise
          only code paths reachable under normal or documented error conditions
    - [ ] No test classes targeting production classes that contain only auto-properties, parameterless
          constructors, or no method bodies — covered indirectly through tests that exercise real behavior
    - [ ] No integration test artifacts in this scope — no `[Trait("Category", "Integration")]` annotations,
          `IAsyncLifetime` container fixtures, or `WebApplicationFactory` usage

4. Organization

    - [ ] Test classes mirror production code structure (e.g., `Configurations/JwtSettingsTests.cs`)
    - [ ] One test class per production class — file name matches with `Tests` suffix
          (e.g., `AuthService` → `AuthServiceTests`) — exception: classes annotated with
          `[ExcludeFromCodeCoverage]` do not require a corresponding test class

5. Log Assertions (fail-open / silent-continue paths)

    - [ ] Use `LogCaptureBuilder.Create<T>()` (from the test project's `Helpers/` folder) to obtain a
          `(Mock<ILogger<T>> Mock, ConcurrentBag<(LogLevel Level, string Message)> Entries)` tuple
    - [ ] Assert both `LogLevel` and a message substring containing at least one context identifier — not just the
          presence of any log entry
    - [ ] Log assertions include at least one context identifier (e.g., idempotency key, username) that proves
          the correct branch ran — no assertions on generic messages that match multiple branches
    - [ ] Do NOT use `Mock.Verify(...)` on `ILogger` — use the captured entries bag instead
    - [ ] Reserve log assertions for paths with no other observable outcome (no return value change, no
          exception, no state mutation) — if a return value or exception already covers the path, a log
          assertion is redundant
