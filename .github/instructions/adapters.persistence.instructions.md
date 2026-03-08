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
    - [ ] `Create` accepts domain entity, extracts fields for INSERT, owns lifecycle defaults
    - [ ] `Update/Save` accepts domain entity, persists mutated state, owns lifecycle updates
    - [ ] Read methods return domain entities — service maps to Views
    - [ ] Paginated queries with `PagedResult<T>`, `PaginationParams`, and SQL `LIMIT/OFFSET` — server-side only
    - [ ] Filters passed from service for dynamic `WHERE` clause construction with `DynamicParameters`

3. Validation & Error Handling

    - [ ] Validate data constraints (uniqueness, referential integrity, foreign-key existence) and throw meaningful
      exceptions
    - [ ] Infrastructure errors do not leak technology details to callers — wrap in standard .NET exception types

4. Boundary Violations

    - [ ] No inline/nested DTO classes — use `Models/` folder
    - [ ] No inline Model-to-Entity mapping — use `Mapping/` folder
    - [ ] No business rules duplicated in SQL
    - [ ] No infrastructure internals exposed to domain or application (e.g., password hashes)
    - [ ] No unbounded list queries for growing datasets — acceptable for naturally bounded datasets (e.g., API
      versions, roles)
