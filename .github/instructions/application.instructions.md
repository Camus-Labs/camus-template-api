---
applyTo: "src/Application/**"
---

# Application Layer Conventions

1. Scope Compliance

    - [ ] Custom attributes target cross-cutting concerns only (e.g., [RateLimit])
    - [ ] Port interfaces abstract all infrastructure interactions — application services depend on interfaces, never
          concrete adapter types
    - [ ] Constants target cross-cutting concerns only (e.g., ErrorCodes, Headers)

2. Type Conventions & Lifecycle

    - [ ] `*Commands.cs` — positional records for write inputs
    - [ ] `*Results.cs` — positional records for write outputs, only when no View matches the shape
    - [ ] `*Filters.cs` — positional records with defaults for query inputs
    - [ ] `*Views.cs` — positional records for query output projections
    - [ ] Views as default return type — Result only when output fields differ from every existing View
    - [ ] View naming describes content/shape (`GeneratedTokenSummaryView`, not `GeneratedTokenByUserView`)
    - [ ] Filter naming targets the entity (`GeneratedTokenFilter`, not `GeneratedTokenSummaryFilter`)
    - [ ] Common types in `Common/` folder: `PaginationParams`, `PagedResult<T>`
    - [ ] List queries returning paginated results with `PaginationParams`/`PagedResult<T>`

3. Validation & Error Handling

    - [ ] Service methods wrap port calls in try-catch
    - [ ] Catch blocks add business operation context to exceptions (e.g., `"Failed to cancel order {orderId}"`)
    - [ ] Domain and validation exceptions re-thrown unchanged — infrastructure failures wrapped
          in `InvalidOperationException` preserving inner exception
    - [ ] Transactional methods: inner catch calls `Rollback()` + `throw;`, outer catch applies the wrapping pattern

4. Observability

    - [ ] Application services set `SetExecutionTags` (business context) via `Activity.Current`
