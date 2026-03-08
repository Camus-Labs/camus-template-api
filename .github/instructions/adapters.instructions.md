---
applyTo: "src/Adapters/**"
---

# Adapters Layer Conventions

1. Scope Compliance

    - [ ] Implementation of Application interfaces (ports)
    - [ ] Technology-specific code (database, caching, Dapr, Redis, JWT, etc.)
    - [ ] External service clients and SDKs
    - [ ] Adapter-specific interfaces (consumed only within the adapter)
    - [ ] Adapter-specific configuration classes (e.g., `JwtSettings`, `RateLimitSettings`)
    - [ ] Extension methods for service registration (e.g., `AddJwtAuthentication()`)
    - [ ] Adapter-specific middleware

2. Type Conventions & Lifecycle

    - [ ] Configuration classes in `Configurations/` folder with `*Settings` suffix
    - [ ] Service implementations in `Services/` or `Handlers/` folder matching the Application interface name
    - [ ] Single setup extension method per adapter (e.g., `JwtSetupExtensions.cs`) for DI registration
    - [ ] Each adapter is independently swappable — no cross-adapter dependencies

3. Validation & Error Handling

    - [ ] Adapter-specific exceptions wrap underlying technology failures with meaningful context
    - [ ] Infrastructure errors do not leak technology details to callers — wrap in standard .NET exception types

4. Boundary Violations

    - [ ] No business/domain logic
    - [ ] No HTTP endpoint definitions
    - [ ] No interfaces consumed by API — move to Application
