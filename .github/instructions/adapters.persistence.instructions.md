---
applyTo: "src/Adapters/emc.camus.persistence.*/**"
---

# Persistence Adapter Conventions

1. Scope Compliance

    - [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`) — Dapper maps rows to these
    - [ ] Mapping extensions in `Mapping/` with `*MappingExtensions` suffix — `ToEntity()` via `Entity.Reconstitute()`
    - [ ] Each Model has a corresponding MappingExtensions class
    - [ ] Repository implementations in `Repositories/` folder

2. Type Conventions & Lifecycle

    - [ ] Lifecycle fields owned by repository: `created_at` via `DEFAULT NOW()`, `updated_at` on UPDATE,
          `created_by`/`updated_by` from `IUserContext`
    - [ ] Entity-centric writes for entities with state transitions — service constructs/mutates entity, repository
          persists (load → mutate → save)
    - [ ] Parameter-based writes for infrastructure operations without domain behavior (e.g., `AuthenticateAsync`
          with BCrypt)
    - [ ] `Create` accepts domain entity and extracts fields for INSERT
    - [ ] `Update/Save` accepts domain entity and persists mutated state
    - [ ] Read methods return domain entities — service maps to Views
    - [ ] Paginated queries with `PagedResult<T>`, `PaginationParams`, and SQL `LIMIT/OFFSET` — server-side only
    - [ ] Dynamic `WHERE` clause construction via `DynamicParameters` from caller-supplied filters

3. Validation & Error Handling

    - [ ] Validate data constraints (uniqueness, referential integrity, foreign-key existence) and throw exceptions that
          identify the constraint and conflicting value

4. Boundary Violations

    - [ ] No inline/nested DTO classes — use `Models/` folder
    - [ ] No inline Model-to-Entity mapping — use `Mapping/` folder
    - [ ] No SQL `CHECK` constraints, triggers, or stored procedures that encode domain invariants
    - [ ] No infrastructure internals exposed to domain or application (e.g., password hashes)
    - [ ] No unbounded list queries for growing datasets — acceptable for naturally bounded datasets (e.g., API
          versions, roles)
