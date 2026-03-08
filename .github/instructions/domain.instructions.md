---
applyTo: "src/Domain/**"
---

# Domain Layer Conventions

1. Scope Compliance

    - [ ] Business entities with private setters on all properties
    - [ ] Value objects (e.g., `Email`, `Money`)
    - [ ] Domain rules and business validation
    - [ ] Domain exceptions for business rule violations
    - [ ] Domain constants
    - [ ] Domain events (if using event-driven patterns)
    - [ ] Extension methods for domain entities

2. Type Conventions & Lifecycle

    - [ ] Public constructor for new entities — validates business attributes, sets initial state, auto-generates ID
      when null
    - [ ] `Reconstitute` static factory for rebuilding from persistence — accepts all fields, skips business validation
    - [ ] Business methods for state transitions (e.g., `Revoke()`) — enforce invariants, the only way to mutate state
      after construction
    - [ ] Business methods that need timestamps accept them as parameters — callers supply the clock value
    - [ ] Lifecycle/audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) are read-only — populated only
      via `Reconstitute`

3. Validation & Error Handling

    - [ ] `InvalidOperationException` for business invariant violations

4. Boundary Violations

    - [ ] No infrastructure dependencies (database, HTTP, file system)
    - [ ] No Application layer references or framework-specific code
    - [ ] No `DateTime.UtcNow` or clock dependencies — timestamps are infrastructure concerns
