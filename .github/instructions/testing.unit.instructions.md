---
applyTo: "{src/Test/**,!src/Test/**integration.test/**}"
---

# Unit Testing Conventions

1. Frameworks & Mocking

    - [ ] Moq as the only mocking framework — no other mocking libraries
    - [ ] Mocks only for interfaces that cross process or I/O boundaries (e.g., database, HTTP, file system, clock)
    - [ ] No mocks for domain logic — test real implementations
    - [ ] Controller tests mock application services — not real implementations
    - [ ] Application-layer tests mock adapters — not real infrastructure
    - [ ] Repository tests mock DataAccess interfaces — not real data stores
    - [ ] No `Mock.Verify*()` on methods whose return value is already captured and asserted
    - [ ] Configure mocks with only the methods the class exercises — no `Setup` for members that no test
          in the class uses

2. Isolation & Setup

    - [ ] Tests are isolated — no shared mutable state, no static mutable fields, no `IClassFixture<T>` mutation
          across tests
    - [ ] Per-test setup in constructor, cleanup via `IDisposable` — no static initializers or manual lifecycle
          management
    - [ ] No `Thread.Sleep()` or `Task.Delay()` — use deterministic time abstractions or controlled waits
    - [ ] Inject `FakeTimeProvider` (from `Microsoft.Extensions.Time.Testing`) to control the clock in unit
          tests
    - [ ] No hardcoded static dates in test data — express all dates relative to the `FakeTimeProvider`
          reference time (e.g., `_timeProvider.GetUtcNow().AddYears(1)`)
    - [ ] No reflection or access to private/internal members — assert on public return values, thrown exceptions,
          or mock interactions
    - [ ] Values shared between the constructor (or shared setup) and test assertions as `private const` or
          `private static readonly` fields
    - [ ] Constant array data as `private static readonly` fields — C# has no `const` array; inlining allocates
          a new array each time
    - [ ] Non-shared values (single-method arrange/assert, constructor filler not verified by any assertion,
          assertion-only literals) stay inline

3. Scope & Coverage

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

    - [ ] When a code path swallows an exception and continues silently (fail-open, graceful degradation),
          assert on the emitted log entry — this is the only observable side-effect proving the path executed
    - [ ] Use `LogCaptureBuilder.Create<T>()` (from the test project's `Helpers/` folder) to obtain a
          `(Mock<ILogger<T>> Mock, ConcurrentBag<(LogLevel Level, string Message)> Entries)` tuple
    - [ ] Assert both `LogLevel` and a meaningful message substring — not just the presence of any log entry;
          include context identifiers (e.g., idempotency key, username) that prove the correct branch ran
    - [ ] Do NOT use `Mock.Verify(...)` on `ILogger` — use the captured entries bag instead
    - [ ] Reserve log assertions for paths with no other observable outcome (no return value change, no
          exception, no state mutation) — if a return value or exception already covers the path, a log
          assertion is redundant
