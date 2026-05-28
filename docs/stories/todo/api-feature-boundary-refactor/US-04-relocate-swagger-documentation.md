# User Story Specification

## Metadata

- Story ID: `US-04`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `relocate-swagger-documentation`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the Swagger/OpenAPI documentation feature relocated from Adapters into the API layer`, so that
`the architectural boundary correctly reflects that Swashbuckle wiring is an ASP.NET Core pipeline
feature, not a swappable infrastructure adapter behind an Application port`.

### Business Value

- Enforces hexagonal architecture discipline: Swagger configuration is API presentation wiring, not an adapter
  implementing a domain port
- Consolidates all API documentation configuration alongside the controllers it documents
- Simplifies the project graph by removing an unnecessary project boundary

### In Scope

- Move all source files from `src/Adapters/emc.camus.documentation.swagger/` into `src/Api/emc.camus.api/` following the
  Configurations/ + Extensions/\*SetupExtensions.cs pattern
- Update `emc.camus.api.csproj` to remove the project reference to `emc.camus.documentation.swagger` and absorb its
  NuGet dependencies (Swashbuckle)
- Remove the `emc.camus.documentation.swagger` project from the solution
- Delete the `src/Adapters/emc.camus.documentation.swagger/` directory
- Update `Program.cs` composition root to wire Swagger from the API project directly
- Maintain identical Swagger UI behavior: same endpoints, same schemas, same security scheme definitions

### Out of Scope

- Changing Swagger configuration schema, versions, or security schemes
- Adding new API documentation features
- Modifying SwaggerExamples/ (already in the API project)
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: All Swagger/OpenAPI source files are located under `src/Api/emc.camus.api/` in appropriate subdirectories
- FR-02: The `emc.camus.documentation.swagger` project is removed from the solution and its directory deleted
- FR-03: The API project no longer holds a project reference to `emc.camus.documentation.swagger`
- FR-04: Swagger DI registration occurs via an extension method in `src/Api/emc.camus.api/Extensions/`
- FR-05: Swashbuckle NuGet packages are referenced directly in `emc.camus.api.csproj`

### Non-Functional Requirements

- Security: No change — Swagger UI access controls remain identical
- Performance: No performance considerations
- Observability: N/A (Swagger has no custom metrics)
- Reliability: Single coordinated release; no intermediate broken state
- Compliance: N/A

### Acceptance Criteria

- AC-01: Solution builds successfully without `emc.camus.documentation.swagger` project reference
- AC-02: All Swagger source files exist under `src/Api/emc.camus.api/` in the correct subdirectories
- AC-03: Swagger UI at `/swagger` renders identically with all configured versions and security schemes
- AC-04: OpenAPI JSON/YAML spec output is functionally equivalent

### Constraints and Dependencies

- Business constraints:
  - Must be delivered after US-03 (sequential ordering)
  - Single coordinated release with US-05
- Dependencies:
  - None beyond NuGet packages (Swashbuckle.AspNetCore)

### Risks and Open Questions

- Risks:
  - Minimal risk — Swagger adapter is typically configuration-only with no complex logic; owner: Tech Lead
- Open questions:
  - None

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- Scope boundaries clear (in | out): `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Dependencies and constraints identified: `Yes`
- Risks and open questions documented: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `Tech Lead, 2026-05-27`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes — Swagger documentation has no domain interaction
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No changes — the Swagger adapter does not implement any Application port interface; its reference to
    Application is read-only (uses constants for auth scheme names)
  - Potential files/folders to touch: `None`
- API
  - Change summary: Absorb Swagger source files into the API project; add a new `SwaggerSetupExtensions.cs` in
    `Extensions/`; add `Configurations/SwaggerSettings.cs` and `Configurations/ApiVersionSettings.cs`; add
    `Filters/DefaultApiResponsesOperationFilter.cs`; update `emc.camus.api.csproj` to include Swashbuckle NuGet packages
    directly and remove the project reference to the Swagger adapter; update `Program.cs` using directive to reference
    the new namespace
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch:
    `src/Api/emc.camus.api/Extensions/SwaggerSetupExtensions.cs`,
    `src/Api/emc.camus.api/Configurations/SwaggerSettings.cs`,
    `src/Api/emc.camus.api/Configurations/ApiVersionSettings.cs`,
    `src/Api/emc.camus.api/Filters/DefaultApiResponsesOperationFilter.cs`,
    `src/Api/emc.camus.api/emc.camus.api.csproj`,
    `src/Api/emc.camus.api/Program.cs`
- Adapters
  - Change summary: Delete the entire `emc.camus.documentation.swagger` project directory and remove it from the
    solution file
  - Potential files/folders to touch: `src/Adapters/emc.camus.documentation.swagger/ (delete), src/CamusApp.sln`
- Database Schema
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- Tests
  - Change summary: Out of scope for this story (covered in US-05); existing test project
    `emc.camus.documentation.swagger.test` remains until US-05 relocates it
  - Potential files/folders to touch: `None (deferred to US-05)`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No decision required — Swagger UI access controls (environment-gated exposure, security scheme definitions)
  are preserved as-is during relocation; no behavioral changes
- Performance: No decision required — no performance-sensitive paths affected
- Observability: No decision required — Swagger adapter has no custom metrics or tracing
- Reliability: Coordinate this story atomically with US-05 (test relocation) in a single release to avoid a broken
  intermediate state where the adapter project is deleted but its test project still references it
- Compliance: No decision required

### Delivery and Rollout Notes

- Rollout strategy: Full rollout in a single coordinated release alongside US-05; no feature flag needed since this is
  an internal structural refactor with no behavioral change
- Rollback strategy: Revert the merge commit to restore the Swagger adapter project and its project reference in the API
  csproj; no data migration or state to reconcile
- Operational readiness checks: Verify Swagger UI renders at `/swagger` after deployment; confirm OpenAPI spec endpoints
  return valid JSON; no new alerts or runbooks required

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Copilot Architect, 2026-05-27`

## Section C - Implementation Tracking

### Test Traceability

| AC    | Test Class      | Test Method                          | Layer                               | Change          |
| ----- | --------------- | ------------------------------------ | ----------------------------------- | --------------- |
| AC-01 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-02 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-03 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |

### Skeleton Inventory

| Layer                               | Stub File             | Change          | Types                      | Members                         |
| ----------------------------------- | --------------------- | --------------- | -------------------------- | ------------------------------- |
| [Domain, Application, Api, Adapter] | [src/.../FileName.cs] | [New, Modified] | [class, interface, record] | [method signatures, properties] |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `[Yes | No]`
- Skeleton inventory complete and user-approved: `[Yes | No]`
- Tests compile and fail for the right reason (TDD red): `[Yes | No]`
- Ready for implementation: `[Yes | No]`
- Tester sign-off: `[Name, Date]`

### Regression Fixes Log

| #   | Test File        | Test Method   | Change Made          | Reason                                  |
| --- | ---------------- | ------------- | -------------------- | --------------------------------------- |
| [n] | [test file path] | [method name] | [description of fix] | [contract change that caused the break] |

### Developer Handoff Gate

- All unit tests pass (TDD green): `[Yes | No]`
- All existing integration tests pass: `[Yes | No]`
- Regression fixes documented (if any): `[Yes | N/A]`
- Build succeeds with zero warnings: `[Yes | No]`
- Ready for code review: `[Yes | No]`
- Developer sign-off: `[Name, Date]`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary               | Factory              | Test Class      | Test Method                          | Change                    |
| ---------------------- | -------------------- | --------------- | ------------------------------------ | ------------------------- |
| [cross-layer boundary] | [factory class name] | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [New, Modified, Existing] |

### Integration Test Findings

| #   | Test          | Failure               | Root Cause Analysis | Affected File          |
| --- | ------------- | --------------------- | ------------------- | ---------------------- |
| [n] | [test method] | [failure description] | [analysis]          | [production file path] |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `[Yes | No]`
- All integration tests pass: `[Yes | No]`
- No unresolved production code findings: `[Yes | No]`
- Ready for review: `[Yes | No]`
- Integration Tester sign-off: `[Name, Date]`

## Section E - Technical Writer

### Version Update

- Previous version: `[X.X.X]`
- New version: `[X.X.X]`
- Bump type: `[MAJOR | MINOR | PATCH | APPEND]`
- Reason: `[one-sentence justification]`

### CHANGELOG Entry
