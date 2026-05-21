---
applyTo: "src/Adapters/**/*.cs"
---

# Adapters Layer Conventions

1. Scope Compliance

    - [ ] No business rules or domain logic — those belong in Domain entities
    - [ ] No layer orchestration (coordinating domain entities and port calls)
    - [ ] File implements an Application-layer interface (port), or is a configuration, extension, mapping,
          or `internal` helper class consumed by one that does
    - [ ] External service clients wrap a single SDK or service
    - [ ] Adapter-internal types (classes and interfaces) not consumed outside the adapter are `internal`
          — `public` interfaces belong in Application
    - [ ] Middleware is adapter-specific and not shared across adapters

2. Type Conventions & Lifecycle

    - [ ] Service implementations live in `Services/` or `Handlers/` folder matching the Application interface name
          — exception: repository services in `Repositories/`
    - [ ] Setup extension method lives in a single `*SetupExtensions.cs` file per adapter for DI registration
    - [ ] No cross-adapter dependencies — referencing another adapter creates hidden coupling that defeats swappability

3. Validation & Error Handling

    - [ ] Technology failures (timeouts, connection refused, 5xx) from direct SDK or infrastructure calls are
          wrapped in a custom `internal` adapter exception living in `Exceptions/` folder, preserving the inner
          exception — exception: calls to Application-layer ports (interfaces implemented by other adapters)
          propagate exceptions as-is because the owning adapter is responsible for its own wrapping
    - [ ] Logical failures from external services (HTTP 404 from a remote API, malformed response body,
          unexpected payload shape) throw `InvalidOperationException` — exception: repository entity-not-found
          lookups throw `KeyNotFoundException` to signal a missing entity to the caller

4. Code Coverage Exclusions

    - [ ] `[ExcludeFromCodeCoverage]` on classes whose public methods exclusively delegate to external
          infrastructure SDK calls with no branching logic (e.g., Dapper queries on `DbConnection`, Dapr client
          calls, blob storage operations)
