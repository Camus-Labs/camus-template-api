---
applyTo: "src/Domain/**/*.cs"
---

# Domain Layer Conventions

1. Scope Compliance

    - [ ] All entity properties use private setters
    - [ ] No `<ProjectReference>` elements targeting projects outside `src/Domain/`
    - [ ] No `<PackageReference>` elements — zero NuGet dependencies

2. Type Conventions & Lifecycle

    - [ ] Public constructor for new entities — the only way to create a new instance
    - [ ] Public constructor validates business attributes before setting state
    - [ ] Public constructor auto-generates `Id` when the caller passes null
    - [ ] `Reconstitute` static factory for rebuilding from persistence — bypasses business validation
    - [ ] `Reconstitute` accepts all fields including lifecycle/audit data
    - [ ] Business methods for state transitions (e.g., `Revoke()`) — the only way to mutate state after construction
    - [ ] Business methods that mutate state include guard clauses before any field assignment
    - [ ] Business methods that need timestamps accept them as parameters — callers supply the clock value
    - [ ] Lifecycle/audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) are read-only — populated only
          via `Reconstitute`

3. Validation & Error Handling

    - [ ] Business invariant violations throw `InvalidOperationException`
