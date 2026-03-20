---
applyTo: "{src/**/*.cs,!src/Test/**}"
---

# C# Coding Standards

1. Code Quality

    - [ ] Numeric literals other than 0, 1, -1 that represent domain constraints, thresholds, or configuration defaults
          appear as named const or static readonly fields — arbitrary sample values (e.g., Swagger example providers)
          stay inline
    - [ ] String literals assigned as property or field default values appear as named const or static readonly fields
          — all other string literals stay inline at the call site
    - [ ] XML documentation on all public types and members
    - [ ] No `<inheritdoc />` — every public type and member has its own explicit XML documentation
    - [ ] No dead code — no unused private constructors, methods, fields, properties, or variables

2. Validation & Error Handling

    - [ ] All public methods/constructors validate parameters — exceptions: mapper methods (pure structural
          transformers), middleware parameters supplied by the ASP.NET Core pipeline, controller action parameters
          bound by `[ApiController]`, DI setup extension method parameters, `Reconstitute` static factories, and
          exception type constructors
    - [ ] `Argument*Exception.ThrowIf*()` static helpers replace manual `if`/`throw` when a matching helper exists —
          applies to all validation code (public entry points and private `Validate*` methods alike)
    - [ ] Validation methods throw exceptions — never return null/false
    - [ ] Multi-statement validation on non-settings classes as `private void Validate{Property}()` methods
    - [ ] Exception throw statements contain the offending value or the violated constraint

3. Configuration Classes (`*Settings`)

    - [ ] `*Settings` suffix on all configuration/options classes (e.g., `JwtSettings`, `RateLimitSettings`)
    - [ ] Configuration classes live in `Configurations/` folder of the layer they configure
    - [ ] Enums for type-safe options — exception: validated strings for framework-mandated identifiers
    - [ ] `Validate{Property}()` methods called from a central `Validate()` method
    - [ ] Each property has its own validation method — exception: properties whose type is inherently
          constrained
    - [ ] Validation constants as `private const` fields
    - [ ] Property validation throws `InvalidOperationException` (invalid object state) — never `ArgumentException`
          (`ArgumentException` is reserved for bad method/constructor parameters)
    - [ ] XML exception documentation on `Validate()` method
    - [ ] No separate validator classes — validation lives with the data

4. Logging

    - [ ] LogDebug only for branching decisions, intermediate computation results, and cache key construction
    - [ ] LogInformation only for: startup/shutdown, state transitions, admin operations, scheduled jobs
    - [ ] LogWarning only for degraded-but-recoverable conditions and security-relevant rejections — fallback
          activation, retry attempts, threshold proximity, deprecated code-path execution, rate-limit rejections,
          authentication failures
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
    - [ ] No layer orchestration (coordinating domain entities and port calls) outside `Application/`
    - [ ] No middleware or DI registration outside `Api/` and `Adapters/`
    - [ ] No inline/nested DTO or model classes
    - [ ] Type-conversion extension methods live in `Mapping/` folder of the layer where the conversion belongs —
          `*MappingExtensions` suffix — exception: `Application/` layer co-locates mappers in their topic folder
          (e.g., `Auth/AuthMappingExtensions.cs`) to preserve feature cohesion
    - [ ] Mapper methods are pure structural transformers — they convert shapes, never validate
