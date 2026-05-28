# User Story Specification

## Metadata

- Story ID: `US-03`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `relocate-apikey-security`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want `the API Key security feature relocated from Adapters into the
API layer`, so that `the architectural boundary correctly reflects that API Key authentication is an
ASP.NET Core pipeline feature, not a swappable infrastructure adapter behind an Application port`.

### Business Value

- Enforces hexagonal architecture discipline: API Key handler is auth-scheme wiring, not an adapter implementing a
  domain port
- Consolidates all authentication pipeline configuration in the API project
- Maintains secrets consumption through the true adapter (`emc.camus.secrets.dapr`)

### In Scope

- Move all source files from `src/Adapters/emc.camus.security.apikey/` into `src/Api/emc.camus.api/` following the
  Configurations/ + Handlers/ + Extensions/\*SetupExtensions.cs + Metrics/ pattern
- Update `emc.camus.api.csproj` to remove the project reference to `emc.camus.security.apikey`
- Remove the `emc.camus.security.apikey` project from the solution
- Delete the `src/Adapters/emc.camus.security.apikey/` directory
- Preserve consumption of `emc.camus.secrets.dapr` for API key retrieval
- Update `Program.cs` composition root to wire API Key auth from the API project directly
- Maintain identical HTTP behavior: same 401 responses, same header validation

### Out of Scope

- Changing API Key validation logic or header name
- Modifying the Application-layer contracts
- Modifying `emc.camus.secrets.dapr` adapter
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: All API Key security source files are located under `src/Api/emc.camus.api/` in appropriate subdirectories
- FR-02: The `emc.camus.security.apikey` project is removed from the solution and its directory deleted
- FR-03: The API project no longer holds a project reference to `emc.camus.security.apikey`
- FR-04: API Key DI registration occurs via an extension method in `src/Api/emc.camus.api/Extensions/`
- FR-05: API Key feature continues consuming `ISecretProvider` (from `emc.camus.secrets.dapr`) for key retrieval

### Non-Functional Requirements

- Security: No change — API Key authentication continues protecting endpoints identically
- Performance: No performance considerations
- Observability: Existing API Key auth metrics remain functional with same metric names
- Reliability: Single coordinated release; no intermediate broken state
- Compliance: N/A

### Acceptance Criteria

- AC-01: Solution builds successfully without `emc.camus.security.apikey` project reference
- AC-02: All API Key security source files exist under `src/Api/emc.camus.api/` in the correct subdirectories
- AC-03: API Key authentication validates the `Api-Key` header identically against the secret provider
- AC-04: HTTP responses for API Key auth failures return identical status codes (401) and response bodies

### Constraints and Dependencies

- Business constraints:
  - Must be delivered after US-02 (sequential ordering)
  - Single coordinated release with US-05
- Dependencies:
  - `emc.camus.secrets.dapr` adapter remains unchanged and available
  - Application-layer contracts remain unchanged

### Risks and Open Questions

- Risks:
  - Minimal risk — API Key handler is typically a simple authentication handler with few dependencies; owner: Tech Lead
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
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No changes — existing contracts (`ISecretProvider`, `AuthenticationSchemes`, `ApiKeyConstants`)
    remain unchanged and continue to be consumed
  - Potential files/folders to touch: `None`
- Database Schema
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- API
  - Change summary: Absorb all API Key authentication source files from the adapter into the API project. Add
    `Configurations/ApiKeySettings.cs`, `Handlers/ApiKeyAuthenticationHandler.cs`, and
    `Extensions/ApiKeySetupExtensions.cs`. Update `Program.cs` to resolve the setup extension from the local namespace
    instead of the adapter namespace. Remove the project reference to `emc.camus.security.apikey` from
    `emc.camus.api.csproj`
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch: `src/Api/emc.camus.api/Configurations/ApiKeySettings.cs`,
    `src/Api/emc.camus.api/Handlers/ApiKeyAuthenticationHandler.cs`,
    `src/Api/emc.camus.api/Extensions/ApiKeySetupExtensions.cs`, `src/Api/emc.camus.api/emc.camus.api.csproj`,
    `src/Api/emc.camus.api/Program.cs`
- Adapters
  - Change summary: Remove the entire `emc.camus.security.apikey` project from the solution and delete the
    `src/Adapters/emc.camus.security.apikey/` directory. No changes to `emc.camus.secrets.dapr` or any other adapter
  - Potential files/folders to touch: `src/Adapters/emc.camus.security.apikey/` (deleted), `src/CamusApp.sln`
- Tests
  - Change summary: Out of scope per Section A (covered in US-05). Existing unit test project
    `emc.camus.security.apikey.test` will be relocated in US-05. Integration tests continue to exercise the same HTTP
    behavior via the running API host and require no source changes in this story
  - Potential files/folders to touch: `None (deferred to US-05)`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No behavioral change. The `ApiKeyAuthenticationHandler` retains identical validation logic, continues
  consuming `ISecretProvider` from `emc.camus.secrets.dapr`, and produces the same 401 responses. The handler class
  remains `internal sealed` within the API assembly boundary
- Performance: No impact — the handler is already loaded in the same ASP.NET Core process; eliminating the separate
  assembly reduces one project load at build time with zero runtime difference
- Observability: Existing API Key authentication metrics (if any counters are emitted from the handler) retain the same
  meter and instrument names. No metric name changes are permitted during relocation
- Reliability: Single coordinated commit relocating all files atomically. The solution must build and pass integration
  tests at every commit boundary. No intermediate state where the handler exists in both locations
- Compliance: N/A

### Delivery and Rollout Notes

- Rollout strategy: Full rollout in a single coordinated release alongside US-05 (test relocation). No feature flag
  needed — this is an internal structural change with zero behavioral difference
- Rollback strategy: Revert the commit that removes the adapter project and restores the project reference in
  `emc.camus.api.csproj`. The solution file and adapter directory are restored from version control
- Operational readiness checks: Verify integration tests pass post-merge (API Key header validation returns 401 on
  missing/invalid key and 200 on valid key). Confirm existing observability dashboards show no metric name gaps

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect, 2026-05-27`

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
