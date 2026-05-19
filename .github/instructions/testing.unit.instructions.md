---
applyTo: "{src/Test/**,!src/Test/**integration.test/**}"
---

# Unit Testing Conventions

1. Frameworks & Mocking

    - [ ] Moq as the only mocking framework — no other mocking libraries
    - [ ] Mocks only for interfaces that cross process or I/O boundaries (e.g., database, HTTP, file system, clock)
    - [ ] No mocks for domain logic — test real implementations
    - [ ] Mock application services when testing controllers
    - [ ] Mock adapters when testing application layer
    - [ ] Mock DataAccess interfaces when testing repository implementations
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

    - [ ] Tests in correct project matching production structure (e.g., `emc.camus.security.jwt.test`)
    - [ ] Test classes mirror production code structure (e.g., `Configurations/JwtSettingsTests.cs`)
    - [ ] Each adapter test project name matches its production counterpart (e.g.,
          `emc.camus.security.jwt.test` → `emc.camus.security.jwt`)
    - [ ] One test class per production class — file name matches with `Tests` suffix
          (e.g., `AuthService` → `AuthServiceTests`) — exception: classes annotated with
          `[ExcludeFromCodeCoverage]` do not require a corresponding test class

5. Assertions

    - [ ] Exception messages: wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
    - [ ] Never assert on `exception.Data` — assert on message patterns instead
