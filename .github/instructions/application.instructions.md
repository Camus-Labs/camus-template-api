---
applyTo: "src/Application/**/*.cs"
---

# Application Layer Conventions

1. Scope Compliance

    - [ ] Custom attributes target cross-cutting concerns only (e.g., [RateLimit])
    - [ ] Port interfaces abstract all infrastructure interactions
    - [ ] Application services depend on port interfaces — never concrete adapter types
    - [ ] Constants target cross-cutting concerns only (e.g., ErrorCodes, Headers)

2. Type Conventions & Lifecycle

    - [ ] Write-input records live in `*Commands.cs` as positional records
    - [ ] Write-output records live in `*Results.cs` as positional records — never duplicate an existing View shape
          as a Result
    - [ ] Query-input records live in `*Filters.cs` as positional records with default parameter values
    - [ ] Query-output records live in `*Views.cs` as positional records
    - [ ] Service methods accept Application-layer types only (`*Command`, `*Filter`, `PaginationParams`) — never
          raw primitives or API models
    - [ ] Service methods return View or Result types
    - [ ] View names contain the entity and a content/shape qualifier — never a filter-dimension qualifier
          (`GeneratedTokenSummaryView`, not `GeneratedTokenByUserView`)
    - [ ] Filter naming targets the entity (`GeneratedTokenFilter`, not `GeneratedTokenSummaryFilter`)
    - [ ] Common types in `Common/` folder: `PaginationParams`, `PagedResult<T>`
    - [ ] List query methods accept `PaginationParams` and return `PagedResult<T>`

3. Validation & Error Handling

    - [ ] Contract constructors (`*Command`, `*Filter`, `*Results`, `*Views`) are the authoritative validation gate —
          an instance that exists is guaranteed valid ("parse, don't validate")
    - [ ] Constructor validation checks null, empty, format, range, and type coercion — no business rules
          (those belong in Domain entities)
    - [ ] Service methods wrap port calls in try-catch
    - [ ] Catch blocks add business operation context to exceptions (e.g., `"Failed to cancel order {orderId}"`)
    - [ ] Domain and validation exceptions re-thrown unchanged
    - [ ] Infrastructure failures wrapped in `InvalidOperationException` preserving inner exception
    - [ ] Transactional methods: inner catch calls `Rollback()` + `throw;`, outer catch wraps in
          `InvalidOperationException` preserving inner exception

4. Observability

    - [ ] Application services set `SetExecutionTags` (business context) via `Activity.Current`
