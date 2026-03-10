---
applyTo: "src/Adapters/**"
---

# Adapters Layer Conventions

1. Scope Compliance

    - [ ] File implements an Application-layer interface (port), or is a configuration, extension, or mapping
          class consumed by one that does
    - [ ] External service clients wrap a single SDK or service
    - [ ] Adapter-internal interfaces are `internal` — `public` interfaces belong in Application
    - [ ] Middleware is adapter-specific and not shared across adapters

2. Type Conventions & Lifecycle

    - [ ] Service implementations live in `Services/` or `Handlers/` folder matching the Application interface name
          except repository services which live in `Repositories/`
    - [ ] Setup extension method lives in a single `*SetupExtensions.cs` file per adapter for DI registration
    - [ ] No cross-adapter dependencies — each adapter references only Application-layer interfaces

3. Validation & Error Handling

    - [ ] Adapter-specific exceptions wrap underlying technology failures — include operation name and preserve inner
          exception
    - [ ] Infrastructure errors do not leak technology details to callers
