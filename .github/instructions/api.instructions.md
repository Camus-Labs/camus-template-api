---
applyTo: "src/Api/**/*.cs"
---

# API Layer Conventions

1. Scope Compliance

    - [ ] Controllers call application services with Application-layer types only (`*Command`, `*Filter`,
          `PaginationParams`) — never raw primitives or API models
    - [ ] `[FromBody]` / `[FromQuery]` parameters converted via extension methods on the request type (`ToCommand()`,
          `ToFilter()`, `ToPaginationParams()`)
    - [ ] Primitive route parameters converted via static mapper methods (e.g.,
          `AuthMappingExtensions.ToRevokeTokenCommand(jti)`) — never extension methods on built-in types
    - [ ] Controllers return `Models/` types or `IActionResult` — no domain entities or Application views
    - [ ] Controller parameters use model binding attributes only (`[FromBody]`, `[FromQuery]`, `[FromRoute]`)
    - [ ] DI registration lives in `Extensions/` folder as one `*SetupExtensions.cs` file per concern
    - [ ] `Infrastructure/` folder for framework-dependent service implementations (e.g., `HttpUserContext`
          implementing `IUserContext`) — distinct from `Models/` (data shapes) and `Mapping/` (converters)

2. Type Conventions & Lifecycle

    - [ ] DTO classes in folder: `Models/Dtos/`
    - [ ] Response envelopes in folder: `Models/Responses/`
    - [ ] Input models in folder: `Models/Requests/`
    - [ ] Versioned types in version folders where they originate (e.g., `Models/Dtos/V1/`, `Models/Requests/V2/`)
    - [ ] Version folders exist only when a type adds, removes, or renames properties relative to the prior version —
          identical shapes use the original version's type
    - [ ] Shared infrastructure types unversioned in parent folder (e.g., `PaginationQuery`, `ApiResponse<T>`,
          `PagedResponse<T>`)
    - [ ] Version independence — V2 types never inherit from or reference V1 types
    - [ ] Feature-specific mappers in version folders (e.g., `Mapping/V1/ApiInfoMappingExtensions`,
          `Mapping/V2/AuthMappingExtensions`)
    - [ ] Reusable mappers unversioned (`Mapping/CommonMappingExtensions` — `ToPaginationParams()`,
          `ToPagedResponse()`)
    - [ ] Every `[FromBody]` request type and every `[ProducesResponseType]` response type has a corresponding
          `IExamplesProvider<T>` class in `SwaggerExamples/V{n}/`
    - [ ] `IExamplesProvider<T>.GetExamples()` sets every public property of `T` — added or renamed properties in the
          model require a matching update in the example class
    - [ ] `[ProducesResponseType]` for success responses only (200, 201, 204) with typed payloads — never for error
          responses (`DefaultApiResponsesOperationFilter` adds these globally)

3. Validation & Error Handling

    - [ ] Mapper methods are pure structural transformers — they convert shapes, never validate
    - [ ] Controllers contain zero validation logic
    - [ ] No try/catch in controllers — exceptions propagate to the global error-handling middleware
    - [ ] No validation attributes on model classes (`[Required]`, `[StringLength]`, `[Range]`)

4. Observability

    - [ ] Controllers call `SetRequestTags` with one or more keys
    - [ ] Controllers call `SetResponseTags` with one or more keys
