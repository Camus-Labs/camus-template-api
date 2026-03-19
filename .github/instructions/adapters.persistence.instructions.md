---
applyTo: "src/Adapters/emc.camus.persistence.postgresql/**/*.cs"
---

# Persistence Adapter Conventions

1. Scope Compliance

    - [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`) — Dapper maps rows to these
    - [ ] Mapping extensions convert via `ToEntity()` calling `Entity.Reconstitute()`
    - [ ] Each Model has a corresponding MappingExtensions class

2. Type Conventions & Lifecycle

    - [ ] Repository sets `created_by`/`updated_by` from `IUserContext` via database session variable
          (`app.current_username`) — `NpgsqlConnectionFactory.SetSessionContextAsync` propagates the value
          on connection open, and database triggers populate the audit columns automatically
    - [ ] `Create` accepts a domain entity and extracts fields for INSERT — no domain entity instantiation
          inside the repository
    - [ ] `Update/Save` accepts a domain entity and persists mutated state
    - [ ] Write methods that accept scalar parameters operate directly on those values (e.g., `AuthenticateAsync`
          with BCrypt) — no domain entity loading or mutation
    - [ ] Read methods return domain entities via `Mapping/` extensions, not Models or DTOs
    - [ ] Paginated queries accept `PaginationParams` and return `PagedResult<T>`
    - [ ] Pagination uses SQL `LIMIT/OFFSET` — no client-side filtering of full result sets
    - [ ] Dynamic `WHERE` clause construction via `DynamicParameters` from caller-supplied filters
    - [ ] No repository SQL strings containing DDL (`CHECK`, `CREATE TRIGGER`, `CREATE PROCEDURE`) that
          enforce domain invariants

3. Validation & Error Handling

    - [ ] Repositories validate data constraints (uniqueness, referential integrity, foreign-key existence)
          before persisting
    - [ ] Constraint violations throw `InvalidOperationException` for uniqueness/integrity or
          `KeyNotFoundException` for missing references
