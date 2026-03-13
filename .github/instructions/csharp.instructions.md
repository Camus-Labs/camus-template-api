---
applyTo: "{src/**/*.cs,!src/Test/**}"
---

# C# Coding Standards

1. Code Quality

    - [ ] Numeric literals other than 0, 1, -1 appear as named const or static readonly fields
    - [ ] String literals other than string.Empty appear as named const or static readonly fields when used more than
          once, represent an external contract (configuration keys, HTTP header names, public API response field
          names), or appear at a call site without a self-documenting named parameter — except (even when repeated):
          dictionary initializer keys, activity/span names, and structured log template parameters
    - [ ] XML documentation on all public types and members

2. Validation & Error Handling

    - [ ] All public methods/constructors validate parameters with `ArgumentNullException.ThrowIf*()` static helpers
          — no manual `if`/`throw` with `nameof()`
    - [ ] `Guid` parameters guard against `Guid.Empty` with `ArgumentException`
    - [ ] Validation methods throw exceptions — never return null/false
    - [ ] Multi-statement validation on non-settings classes as `private void Validate{Property}()` methods
    - [ ] Exception throw statements use string interpolation containing the offending value or the violated constraint

3. Configuration Classes (`*Settings`)

    - [ ] `*Settings` suffix on all configuration/options classes (e.g., `JwtSettings`, `RateLimitSettings`)
    - [ ] Configuration classes live in `Configurations/` folder of the layer they configure
    - [ ] Enums for type-safe options — exception: validated strings for framework-mandated identifiers
    - [ ] `Validate{Property}()` methods called from a central `Validate()` method
    - [ ] Each property has its own validation method
    - [ ] Validation constants as `private const` fields
    - [ ] XML exception documentation on `Validate()` method
    - [ ] No separate validator classes — validation lives with the data

4. Logging

    - [ ] LogDebug only for branching decisions, intermediate computation results, and cache key construction
    - [ ] LogInformation only for: startup/shutdown, state transitions, admin operations, scheduled jobs
    - [ ] LogWarning only for degraded-but-recoverable conditions — fallback activation, retry attempts, threshold
          proximity, deprecated code-path execution
    - [ ] LogError only at catch-and-terminate boundaries (global error-handling middleware, background job runners) —
          not in service methods that re-throw

5. Metrics

    - [ ] Counters for events (requests, failures, cache hits), histograms for durations — match instrument to signal
    - [ ] Low-cardinality labels only (endpoint, status code, error type) — never user ID, request ID, or entity ID
    - [ ] Metrics classes in `Metrics/` folder of the layer they instrument with `*Metrics` suffix (e.g.,
          `RateLimitMetrics`)
    - [ ] Metric instrument names use `snake_case` with `_total` suffix for counters

6. Architectural Boundaries

    - [ ] No business rules or domain logic outside `Domain/`
    - [ ] No direct infrastructure access outside `Adapters/`
    - [ ] No HTTP runtime objects (`HttpContext`, `HttpRequest`, `HttpResponse`) or endpoint definitions outside `Api/`
    - [ ] No layer orchestration (coordinating domain entities and port calls) outside `Application/`
    - [ ] No middleware or DI registration outside `Api/` and `Adapters/`
    - [ ] No inline/nested DTO or model classes
    - [ ] Type-conversion extension methods live in `Mapping/` folder of the layer where the conversion belongs —
          `*MappingExtensions` suffix
