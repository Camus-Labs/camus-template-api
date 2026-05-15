---
applyTo: "src/Adapters/emc.camus.persistence.postgresql/**/*.cs"
---

# Persistence Adapter Conventions

1. Scope Compliance

    - [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`)
    - [ ] DataAccess classes map Dapper rows to `*Model` types — not to domain entities or DTOs
    - [ ] Mapping extensions convert via `ToEntity()` calling `Entity.Reconstitute()`
    - [ ] Each Model has a corresponding MappingExtensions class
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

    - [ ] DataAccess classes are SQL execution wrappers — no validation, branching, or entity conversion
    - [ ] Pagination uses SQL `LIMIT/OFFSET` — no client-side filtering of full result sets
    - [ ] Dynamic `WHERE` clause construction via `DynamicParameters` from caller-supplied filters
    - [ ] No SQL strings containing DDL (`CHECK`, `CREATE TRIGGER`, `CREATE PROCEDURE`) — domain
          invariants belong in `Domain/`, not in persistence SQL

4. Persistence Constraint Enforcement

    - [ ] Repositories validate data constraints (uniqueness, referential integrity, foreign-key existence)
          before persisting
    - [ ] Constraint violations throw `DataConflictException` for uniqueness/integrity or `KeyNotFoundException` for
          missing references
