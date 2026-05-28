---
applyTo: "src/Adapters/emc.camus.persistence.*/**/*.cs"
---

# Persistence Adapter Conventions

1. Scope Compliance

    - [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`)
    - [ ] DataAccess classes map Dapper rows to `*Model` types — not to domain entities or DTOs
    - [ ] Mapping extensions convert via `ToEntity()` calling `Entity.Reconstitute()`
    - [ ] Each Model has a corresponding MappingExtensions class — exception: infrastructure-only
          projections that do not map to domain entities (e.g., schema validation results)
    - [ ] Each DataAccess component defines an `I*DataAccess` interface paired with a `*DataAccess`
          implementation — one pair per repository

2. Repository Responsibilities

    - [ ] Repositories own validation, branching logic, and entity conversion — no direct SQL execution
    - [ ] Repositories delegate all SQL operations to an injected `I*DataAccess` interface
    - [ ] Repository sets `created_by`/`updated_by` from `IUserContext` via database session variable
          (`app.current_username`, `ConnectionFactory.SetSessionContextAsync`) — never obtained by
          querying the database directly
    - [ ] `Create` accepts a domain entity and extracts fields for INSERT — no domain entity instantiation
          inside the repository
    - [ ] `Update/Save` accepts a domain entity and persists mutated state
    - [ ] Write methods that accept scalar parameters operate directly on those values (e.g., `AuthenticateAsync`
          with BCrypt) — no domain entity loading or mutation
    - [ ] Read methods return domain entities via `Mapping/` extensions, not Models or DTOs
    - [ ] Paginated queries accept `PaginationParams` and return `PagedResult<T>`

3. DataAccess Responsibilities

    - [ ] DataAccess classes are SQL execution wrappers with no validation, business-logic branching, or entity
          conversion (exception: conditional SQL generation from caller-supplied parameters, e.g., dynamic
          `WHERE`, `ORDER BY`, `LIMIT/OFFSET`) — SQL execution only
    - [ ] Return types are `*Model` instances, collections thereof, or scalar value types (`bool`, `int`,
          `Guid`, `DateTime`) — never dictionaries or other derived/projected shapes
    - [ ] Pagination uses SQL `LIMIT/OFFSET` — no client-side filtering of full result sets
    - [ ] DataAccess constructs dynamic `WHERE`/`ORDER BY` clauses via `DynamicParameters` from caller-supplied filters
    - [ ] No SQL strings containing DDL (`CHECK`, `CREATE TRIGGER`, `CREATE PROCEDURE`) — domain
          invariants belong in `Domain/`, not in persistence SQL

4. Persistence Constraint Enforcement

    - [ ] Repositories validate data constraints (uniqueness, referential integrity, foreign-key existence)
          before persisting
    - [ ] Constraint violations throw `DataConflictException` for uniqueness or integrity failures

5. Code Coverage Exclusions

    - [ ] `[ExcludeFromCodeCoverage(Justification = "...")]` on DataAccess classes with conditional SQL
          generation — Dapper makes `IDbConnection` impractical to mock; integration tests provide coverage
