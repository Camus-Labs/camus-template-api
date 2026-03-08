---
applyTo: "src/Adapters/**"
---

# Adapters Layer Conventions

1. Scope Compliance

    - [ ] File implements an Application-layer interface (port) or supports one that does
    - [ ] Code is technology-specific (database, caching, Dapr, Redis, JWT, etc.)
    - [ ] External service clients wrap a single SDK or service
    - [ ] Adapter-internal interfaces are consumed only within the same adapter
    - [ ] Configuration classes live in `Configurations/` folder
    - [ ] Service registration uses a single setup extension method per adapter (e.g., `AddJwtAuthentication()`)
    - [ ] Middleware is adapter-specific and not shared across adapters

2. Type Conventions & Lifecycle

    - [ ] Service implementations live in `Services/` or `Handlers/` folder matching the Application interface name
    - [ ] Setup extension method lives in a single `*SetupExtensions.cs` file per adapter for DI registration
    - [ ] Each adapter is independently swappable — no cross-adapter dependencies

3. Validation & Error Handling

    - [ ] Adapter-specific exceptions wrap underlying technology failures with meaningful context
    - [ ] Infrastructure errors do not leak technology details to callers — wrap in standard .NET exception types

4. Boundary Violations

    - [ ] No business/domain logic
    - [ ] No HTTP endpoint definitions
    - [ ] No interfaces consumed by API — move to Application
