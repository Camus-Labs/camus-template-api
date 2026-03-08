---
applyTo: "src/Application/**"
---

# Application Layer Conventions

1. Scope Compliance

    - [ ] Custom attributes for cross-cutting concerns (e.g., `[RateLimit]`)
    - [ ] Custom exceptions for infrastructure failures (e.g., `RateLimitExceededException`)
    - [ ] Port interfaces for adapters with multiple implementations or consumed directly by the API layer
    - [ ] Constants for cross-cutting concerns (e.g., `ErrorCodes`, `Headers`)
    - [ ] Application services as concrete classes — no interface unless multiple implementations exist
    - [ ] Application services orchestrate adapters, repositories, and cross-cutting concerns

2. Type Conventions & Lifecycle

    - [ ] `*Commands.cs` — positional records for write inputs
    - [ ] `*Results.cs` — positional records for write outputs, only when no View matches the shape
    - [ ] `*Filters.cs` — positional records with defaults for query inputs
    - [ ] `*Views.cs` — positional records for query output projections
    - [ ] Views as default return type — Result only when output has a genuinely different shape
    - [ ] View naming describes content/shape (`GeneratedTokenSummaryView`, not `GeneratedTokenByUserView`)
    - [ ] Filter naming targets the entity (`GeneratedTokenFilter`, not `GeneratedTokenSummaryFilter`)
    - [ ] Common types in `Common/` folder: `PaginationParams`, `PagedResult<T>`
    - [ ] Private mapping helpers in services (e.g., `ToSummaryView`) for entity → view conversion

3. Validation & Error Handling

    - [ ] Application services validate workflow rules and orchestration constraints (e.g., "order can only be
      cancelled if not shipped") — distinct from domain invariants enforced by entities
    - [ ] Complex multi-rule validation in `private static void Validate*(...)` helpers — throw, never return
    - [ ] Nullable context values use null-coalescing throw (`?? throw new InvalidOperationException("...")`)
    - [ ] Two-tier catch: re-throw domain/validation exceptions unchanged, wrap infrastructure failures
          in `InvalidOperationException` preserving inner exception
    - [ ] Transactional methods: inner catch calls `Rollback()` + `throw;`, outer catch applies two-tier pattern
    - [ ] Custom exceptions only for cross-cutting infrastructure — prefer standard .NET exception types

4. Observability

    - [ ] Application services set `SetExecutionTags` (business context) via `Activity.Current`

5. Boundary Violations

    - [ ] No HTTP runtime objects (`HttpContext`, `HttpRequest`, `HttpResponse`)
    - [ ] No infrastructure implementations (database, file I/O, caching, secrets)
    - [ ] No interfaces for single-implementation application services
    - [ ] No middleware or DI registration
    - [ ] No domain invariants or entity-level business rules — those belong in Domain entities
    - [ ] No factory methods with `init` setters on value objects
    - [ ] No unbounded list queries for growing datasets without pagination
