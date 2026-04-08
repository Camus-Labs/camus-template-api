---
applyTo: "src/Application/**/*.cs"
---

# Application Layer Conventions

1. Scope Compliance

    - [ ] Custom attributes target cross-cutting concerns only (e.g., [RateLimit])
    - [ ] Driven port interfaces abstract all infrastructure interactions (e.g., `IUserRepository`,
          `ITokenGenerator`)
    - [ ] Driving port interfaces abstract all application service contracts (e.g., `IAuthService`,
          `IApiInfoService`) — concrete service classes implement these interfaces
    - [ ] Application services depend on port interfaces — never concrete adapter types
    - [ ] Constants target cross-cutting concerns only (e.g., ErrorCodes, Headers)

2. Type Conventions & Lifecycle

    - [ ] Write-input records live in `*Commands.cs` as non-positional sealed records
    - [ ] Write-output records live in `*Results.cs` as non-positional sealed records — never duplicate an existing View
          shape as a Result
    - [ ] Query-input records live in `*Filters.cs` as non-positional sealed records with default parameter values —
          exception: required lookup keys (e.g., entity ID, version) that identify a specific entity stay required
    - [ ] Query-output records live in `*Views.cs` as non-positional sealed records
    - [ ] Service method parameters are Application-layer types (`*Command`, `*Filter`, `PaginationParams`) — never
          raw primitives, domain entities, or API models
    - [ ] Service methods return View or Result types
    - [ ] View names contain the entity and a content/shape qualifier — never a filter-dimension qualifier
          (`GeneratedTokenSummaryView`, not `GeneratedTokenByUserView`)
    - [ ] Filter naming targets the entity (`GeneratedTokenFilter`, not `GeneratedTokenSummaryFilter`)
    - [ ] Common types in `Common/` folder: `PaginationParams`, `PagedResult<T>`
    - [ ] List query methods accept `PaginationParams` and return `PagedResult<T>`

3. Validation & Error Handling

    - [ ] Contract constructors (`*Command`, `*Filter`, `*Result`, `*View`) are the authoritative validation gate —
          an instance that exists is guaranteed valid ("parse, don't validate")
    - [ ] Constructor validation checks null, empty, format, range, and type coercion — no business rules
          (those belong in Domain entities)
    - [ ] Service methods contain no `Validate*` helpers or inline validation
    - [ ] Contract shaping (null, format, range) lives in record constructors (`*Command`, `*Filter`, `*Result`,
          `*View`) — business constraints (limits, ranges, windows, authorization rules, invariants) never appear in
          Application-layer constructors
    - [ ] Service methods wrap port calls in try-catch
    - [ ] Precondition checks (parameter validation, context availability) run before the try-catch — only
          port/infrastructure calls go inside
    - [ ] Catch blocks add business operation context to exceptions (e.g., `"Failed to cancel order {orderId}"`)
    - [ ] Exception filters in catch blocks list only domain and validation exception types (`DomainException`,
          `ArgumentException`, `UnauthorizedAccessException`, `KeyNotFoundException`, `DataConflictException`) — never
          `InvalidOperationException` (reserved for infrastructure failure wrapping)
    - [ ] Domain and validation exceptions caught by exception filters are re-thrown unchanged — never wrapped or
          swallowed
    - [ ] Infrastructure failures wrapped in `InvalidOperationException` preserving inner exception
    - [ ] Transactional methods: inner catch calls `Rollback()` + `throw;`, outer catch wraps in
          `InvalidOperationException` preserving inner exception

4. Observability

    - [ ] Application services set `SetExecutionTags` for values calculated or resolved during service processing —
          neither input nor output values
    - [ ] Application services never call `SetRequestTags` — reserved for the inbound adapter layer
    - [ ] Application services never call `SetResponseTags` — reserved for the inbound adapter layer
