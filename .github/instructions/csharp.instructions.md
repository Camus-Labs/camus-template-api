---
applyTo: "{src/**/*.cs,!src/Test/**}"
---

# C# Coding Standards

1. Code Quality

    - [ ] Numeric literals other than 0, 1, -1 that represent domain constraints, thresholds, or configuration
          defaults appear as named const or static readonly fields — arbitrary sample values (e.g., Swagger example
          providers) stay inline
    - [ ] String literals assigned as property or field default values appear as named const or static readonly fields
          — all other string literals stay inline at the call site
    - [ ] XML documentation on all public types and members
    - [ ] No `<inheritdoc />` — explicit documentation captures the type's own contract independent of base hierarchy
    - [ ] No dead code — no unused private constructors, methods, fields, properties, or variables
    - [ ] All async methods on interfaces and their implementations accept `CancellationToken ct = default` as the
          last parameter — exception: operations that must run to completion (fire-and-forget, observability wrappers,
          compensating actions)
    - [ ] Async methods use async overloads of library and framework APIs — never synchronous versions
    - [ ] Every awaited call that accepts `CancellationToken` receives it — never silently dropped
    - [ ] Compensating actions (rollback, cleanup, dispose) omit `CancellationToken` from their signature or
          pass `CancellationToken.None` internally — they must run to completion even after cancellation
    - [ ] No `out` parameters on method signatures
    - [ ] No tuples as return types or parameters on method signatures

2. Validation & Error Handling

    - [ ] All public methods/constructors validate parameters — exceptions: mapper methods (pure structural
          transformers), middleware parameters supplied by the ASP.NET Core pipeline, controller action parameters
          bound by `[ApiController]`, `this` parameters on extension methods, `Reconstitute` static factories,
          and exception type constructors
    - [ ] `Argument*Exception.ThrowIf*()` static helpers replace manual `if`/`throw` when a matching helper exists —
          applies to all validation code (public entry points and private `Validate*` methods alike)
    - [ ] Validation methods throw exceptions — never return null/false
    - [ ] Multi-statement validation on non-settings classes as `private void Validate{Property}()` methods
    - [ ] Exception throw statements contain the offending value, the violated constraint, or the operation name
    - [ ] Exception message expressions contain no operations that can throw
    - [ ] Catch blocks that perform the same action for multiple exception types use a single
          `catch (Exception ex) when (ex is not ...)` filter listing passthrough types — exception: try blocks
          whose awaited calls cannot throw any passthrough type (e.g., compensating actions passing
          `CancellationToken.None`) and purely synchronous try blocks may use an unfiltered
          `catch (Exception ex)`
    - [ ] No separate catch blocks for distinct exception types unless each performs a different action before
          unconditionally re-throwing (e.g., tagging a span as cancelled vs. failed)
    - [ ] `OperationCanceledException` is always a passthrough type in any method that accepts `CancellationToken`
          — never caught, wrapped, or converted to a return value

3. Configuration Classes (`*Settings`)

    - [ ] `*Settings` suffix on all configuration/options classes (e.g., `JwtSettings`, `InMemoryRateLimitingSettings`)
    - [ ] Configuration classes live in `Configurations/` folder of the layer they configure
    - [ ] Enum property type for configuration options that select a code path (e.g., strategy, mode, provider)
    - [ ] Validated const-string set for external-contract identifiers (e.g., framework scheme names, permission keys,
          component names) — validate the property value against the known set
    - [ ] Plain string property type for descriptive pass-through values that flow to other layers without
          conditional logic depending on them
    - [ ] A central `Validate()` method calls each `Validate{Property}()` method
    - [ ] Each property has its own validation method — exception: properties of type `bool`
    - [ ] Validation constants as `private const` fields
    - [ ] Property validation throws `InvalidOperationException` (invalid object state) — never `ArgumentException`
          (`ArgumentException` is reserved for bad method/constructor parameters)
    - [ ] XML exception documentation on `Validate()` method
    - [ ] No separate validator classes — validation lives with the data

4. Logging

    - [ ] LogDebug only for branching decisions, intermediate computation results, cache key construction,
          and skipped operations in no-op implementations
    - [ ] LogInformation only for: startup/shutdown, state transitions, admin operations, audit records, scheduled jobs
    - [ ] LogWarning only for degraded-but-recoverable conditions and security-relevant rejections where execution
          continues without throwing (e.g., fallback activation, retry attempts, threshold proximity, deprecated
          code-path execution, rate-limit rejections, authentication failures)
    - [ ] LogError only at catch-and-terminate boundaries (global error-handling middleware, background job runners) —
          not in service methods that re-throw

5. Metrics

    - [ ] Counters for events (requests, failures, cache hits), histograms for durations — match instrument to signal
    - [ ] Low-cardinality labels only (endpoint, status code, error type) — never user ID, request ID, or entity ID
    - [ ] Metrics classes in `Metrics/` folder of the layer they instrument with `*Metrics` suffix (e.g.,
          `RateLimitMetrics`)
    - [ ] Metric instrument names use `snake_case` with `_total` suffix for counters

6. Code Coverage Exclusions

    - [ ] `[ExcludeFromCodeCoverage]` on pure data-carrier classes with no logic (e.g., database models, DTOs,
          request/response records with only auto-properties)
    - [ ] `[ExcludeFromCodeCoverage]` on DI registration and startup wiring classes (e.g., `*ServiceExtensions`)
    - [ ] `[ExcludeFromCodeCoverage]` on files containing an `<auto-generated>` comment or `[GeneratedCode]` attribute
    - [ ] `[ExcludeFromCodeCoverage(Justification = "...")]` with a non-empty justification on classes that
          contain branching logic but are impractical to unit-test because their entry points cannot be invoked
          without framework-internal context that has no public construction API (e.g., Swagger operation filters
          receiving `OperationFilterContext`, middleware whose logic depends on pipeline-internal state) — classes
          testable via public initialization APIs (`InitializeAsync`, `DefaultHttpContext`, etc.) do not qualify

7. Architectural Boundaries

    - [ ] No inline/nested DTO or model classes
    - [ ] Type-conversion extension methods live in `Mapping/` folder of the layer where the conversion belongs —
          exception: `Application/` layer co-locates mappers in their topic folder (e.g.,
          `Auth/AuthMappingExtensions.cs`) to preserve feature cohesion
    - [ ] Type-conversion extension method classes use `*MappingExtensions` suffix
    - [ ] Mapper methods are pure structural transformers — they convert shapes, never validate — exception:
          reading derived state from the source entity (e.g., `entity.IsActive()`) is allowed when it computes
          a value from that entity's own properties with no side effects or external dependencies
