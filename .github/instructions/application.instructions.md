---
applyTo: "src/Application/**"
---

# Application Layer Conventions

1. Scope Compliance

   - [ ] Custom attributes for cross-cutting concerns (e.g., `[RateLimit]`)
   - [ ] Custom exceptions for infrastructure failures (e.g., `RateLimitExceededException`)
   - [ ] Port interfaces for adapters with multiple implementations (e.g., `ITokenGenerator`)
   - [ ] Interfaces consumed by API layer or multiple adapters
   - [ ] Constants for cross-cutting concerns (e.g., `ErrorCodes`, `Headers`)
   - [ ] Application services as concrete classes — no interface unless multiple implementations exist

2. CQRS Type Conventions

   - [ ] `*Commands.cs` — positional records for write inputs
   - [ ] `*Results.cs` — positional records for write outputs, only when no View matches the shape
   - [ ] `*Filters.cs` — positional records with defaults for query inputs
   - [ ] `*Views.cs` — positional records for query output projections
   - [ ] Views as default return type — Result only when output has a genuinely different shape
   - [ ] View naming describes content/shape (`GeneratedTokenSummaryView`, not `GeneratedTokenByUserView`)
   - [ ] Filter naming targets the entity (`GeneratedTokenFilter`, not `GeneratedTokenSummaryFilter`)

3. Common Types and Validation

   - [ ] Common types in `Common/` folder: `PaginationParams`, `PagedResult<T>`
   - [ ] Constructor-based validation for value objects — no `init` setters that bypass validation
   - [ ] Private mapping helpers in services (e.g., `ToSummaryView`) for entity → view conversion

4. Boundary Violations

   - [ ] No HTTP runtime objects (`HttpContext`, `HttpRequest`, `HttpResponse`)
   - [ ] No infrastructure implementations (database, file I/O, caching, secrets)
   - [ ] No configuration classes (`Settings`, `Options`)
   - [ ] No interfaces for single-implementation application services
   - [ ] No middleware or DI registration
   - [ ] No business/domain logic
   - [ ] No factory methods with `init` setters on value objects
   - [ ] No unbounded list queries for growing datasets without pagination

5. Cross-Cutting Standards

   - [ ] All public methods/constructors validate with `ArgumentNullException.ThrowIfNull()` /
     `ArgumentException.ThrowIfNullOrWhiteSpace()`
   - [ ] Validation methods throw exceptions — never return null/false
   - [ ] No magic numbers/strings — use constants
   - [ ] No duplicate code/logic across files
   - [ ] XML documentation on all public APIs
