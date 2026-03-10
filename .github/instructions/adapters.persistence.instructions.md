---
applyTo: "src/Adapters/emc.camus.persistence.postgresql/**"
---

# Persistence Adapter Conventions

1. Scope Compliance

    - [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`) — Dapper maps rows to these
    - [ ] Mapping extensions convert via `ToEntity()` calling `Entity.Reconstitute()`
    - [ ] Each Model has a corresponding MappingExtensions class

2. Type Conventions & Lifecycle

    - [ ] Repository sets `created_by`/`updated_by` from `IUserContext` on INSERT and UPDATE
    - [ ] `Create` accepts a domain entity and extracts fields for INSERT — no domain entity instantiation
          inside the repository
    - [ ] `Update/Save` accepts a domain entity and persists mutated state
    - [ ] Write methods that accept scalar parameters operate directly on those values (e.g., `AuthenticateAsync`
          with BCrypt) — no domain entity loading or mutation
    - [ ] Read methods return domain entities via `Mapping/` extensions, not Models or DTOs
    - [ ] Paginated queries with `PagedResult<T>`, `PaginationParams`, and SQL `LIMIT/OFFSET` — server-side only
    - [ ] Dynamic `WHERE` clause construction via `DynamicParameters` from caller-supplied filters
    - [ ] No repository SQL strings containing DDL (`CHECK`, `CREATE TRIGGER`, `CREATE PROCEDURE`) that
          enforce domain invariants
    - [ ] No infrastructure internals exposed to domain or application (e.g., password hashes)

3. Validation & Error Handling

    - [ ] Validate data constraints (uniqueness, referential integrity, foreign-key existence) and throw
          `InvalidOperationException` or `KeyNotFoundException`
