---
applyTo: "src/Domain/**"
---

# Domain Layer Conventions

1. Scope Compliance

    - [ ] All entity properties use private setters
    - [ ] Collection properties expose `IReadOnlyList<T>` — prevents external mutation
    - [ ] No `<ProjectReference>` elements targeting projects outside `src/Domain/`
    - [ ] No `<PackageReference>` elements — zero NuGet dependencies

2. Type Conventions & Lifecycle

    - [ ] Public constructor for new entities — the only way to create a new instance
    - [ ] Public constructor validates business attributes before setting state
    - [ ] Public constructor auto-generates `Id` when the caller passes null
    - [ ] `Reconstitute` static factory is required only when persistence must bypass constructor validation
    - [ ] `Reconstitute`, when present, is the only way to bypass business rules — no other public API skips validation
    - [ ] `Reconstitute`, when present, accepts all fields including lifecycle/audit data
    - [ ] Business methods for state transitions (e.g., `Revoke()`) — the only way to mutate state after construction
    - [ ] Business methods that mutate state include guard clauses before any field assignment
    - [ ] Lifecycle/audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) are read-only — populated only
          via `Reconstitute`

3. Validation & Error Handling

    - [ ] Business invariant violations throw `InvalidOperationException`
