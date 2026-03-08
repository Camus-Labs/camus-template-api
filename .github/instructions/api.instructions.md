---
applyTo: "src/Api/**"
---

# API Layer Conventions

1. Scope Compliance

    - [ ] All HTTP endpoints defined as controller actions or minimal API handlers
    - [ ] DTOs separate from Application views — versioning-ready contract boundary
    - [ ] Model binding attributes only (`[FromBody]`, `[FromQuery]`, `[FromRoute]`)
    - [ ] All middleware and HTTP pipeline components defined in this layer
    - [ ] All action filters and exception filters defined in this layer
    - [ ] DI and service registration in `Program.cs`
    - [ ] `Infrastructure/` folder for framework-dependent service implementations (e.g., `HttpUserContext`
          implementing `IUserContext`) — distinct from `Models/` (data shapes) and `Mapping/` (converters)

2. Type Conventions & Lifecycle

    - [ ] DTO folder conventions: `Models/Dtos/` for item DTOs, `Models/Responses/` for envelopes,
          `Models/Requests/` for input models
    - [ ] Versioned types in version folders where they originate (e.g., `Models/Dtos/V1/`, `Models/Requests/V2/`)
    - [ ] Version folder created only when a type's shape diverges — if shapes are identical, reuse the original
          version's type
    - [ ] Shared infrastructure types unversioned in parent folder (e.g., `PaginationQuery`, `ApiResponse<T>`,
          `PagedResponse<T>`)
    - [ ] Version independence — V2 types never inherit from or reference V1 types
    - [ ] Feature-specific mappers in version folders (e.g., `Mapping/V1/ApiInfoMappingExtensions`,
          `Mapping/V2/AuthMappingExtensions`)
    - [ ] Reusable mappers unversioned (`Mapping/CommonMappingExtensions` — `ToPaginationParams()`,
          `ToPagedResponse()`)
    - [ ] `[ProducesResponseType]` for success responses only (200, 201, 204) with typed payloads — never for error
          responses (`DefaultApiResponsesOperationFilter` adds these globally)

3. Validation & Error Handling

    - [ ] Validation lives in mapper extensions — controllers call `ToCommand()` / `ToFilter()` /
          `ToPaginationParams()` / `ToResponse()` / `ToDto()` and let mappers validate before the application
          layer is reached
    - [ ] Validation is structural only (format, null-checks) — no business rules
    - [ ] No try/catch in controllers — exceptions propagate to the global error-handling middleware

4. Observability

    - [ ] Controllers set `SetRequestTags` (request data) and `SetResponseTags` (response data)

5. Boundary Violations

    - [ ] No business rules or domain logic in controllers, middleware, or filters — delegate to Application services
    - [ ] No infrastructure implementations (database, secrets, caching)
    - [ ] No domain entities in controllers — map to DTOs
    - [ ] No Application views returned directly — DTOs are the API contract
    - [ ] No validation attributes on DTOs (`[Required]`, `[StringLength]`, `[Range]`) — validation in mapper extensions
