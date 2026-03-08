---
applyTo: "src/**/*.cs"
---

# C# Coding Standards

1. Code Quality

    - [ ] No magic numbers/strings — use constants
    - [ ] No duplicate code/logic across files
    - [ ] XML documentation on all public types and members

2. Validation & Error Handling

    - [ ] All public methods/constructors validate parameters with `ArgumentNullException.ThrowIf*()` static helpers
      instead of redundant `nameof()`
    - [ ] `Guid` parameters guard against `Guid.Empty` with `ArgumentException`
    - [ ] Validation methods throw exceptions — never return null/false
    - [ ] Complex validation as `private static void Validate{Property}()` methods
    - [ ] All exceptions cascade to `ExceptionHandlingMiddleware` for standard `ProblemDetails` output — no layer
      catches and swallows exceptions silently
    - [ ] Exception messages include the invalid value or violated constraint

3. Configuration Classes (`*Settings`)

    - [ ] `*Settings` suffix on all configuration/options classes (e.g., `JwtSettings`, `RateLimitSettings`)
    - [ ] Enums for type-safe options — exception: validated strings for framework-mandated identifiers
    - [ ] `Enum.IsDefined()` validation to catch misconfigurations
    - [ ] Validation logic as private `ValidateXxx()` methods called from `Validate()`
    - [ ] Each property has its own validation method
    - [ ] Validation constants as `private const` fields
    - [ ] XML exception documentation on `Validate()` method
    - [ ] No separate validator classes — validation lives with the data

4. Logging

    - [ ] LogInformation only for: startup/shutdown, state transitions, admin operations, scheduled jobs, lifecycle events
    - [ ] No per-request LogInformation (authentication succeeded, request processed, order created)
    - [ ] No LogInformation for validation failures — exceptions are already clear

5. Metrics

    - [ ] Counters for events (requests, failures, cache hits), histograms for durations — match instrument to signal
    - [ ] Low-cardinality labels only (endpoint, status code, error type) — never user ID, request ID, or entity ID
    - [ ] No duplication of ASP.NET Core built-in metrics (request rate, duration, status codes)
    - [ ] No business events as metrics (order created, email sent) — use structured logging instead
    - [ ] Metrics classes in `Metrics/` folder with `*Metrics` suffix (e.g., `RateLimitMetrics`)
    - [ ] Meter names use `MeterNames` constants from Application layer — no hardcoded meter name strings
    - [ ] Metric instrument names use `snake_case` with `_total` suffix for counters

6. Boundary Violations

    - [ ] No dead interfaces/abstractions — remove if consumed by nobody
    - [ ] No configuration for unused features
    - [ ] No future-use abstractions not yet needed
