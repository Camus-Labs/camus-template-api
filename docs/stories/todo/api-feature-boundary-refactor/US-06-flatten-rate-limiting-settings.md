# User Story Specification

## Metadata

- Story ID: `US-06`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `flatten-rate-limiting-settings`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the RateLimitingSettings configuration restructured from an open-ended Policies dictionary to a flat,
fixed-keys layout mirroring RequestTimeoutSettings`, so that `the configuration is self-documenting,
eliminates dictionary validation, and enforces a closed set of policy names at compile time`.

### Business Value

- Configuration becomes self-documenting in `appsettings.json` — explicit property names instead of opaque dictionary
  keys
- Matches the established `RequestTimeoutSettings` convention already in the API project (consistent developer
  experience)
- Eliminates runtime dictionary-key validation — unknown policy names fail at startup via the fixed enum/constant set
- Reduces misconfiguration risk: no more typos in policy names that silently create unused policies

### In Scope

- Replace `Policies` dictionary in `RateLimitingSettings` with flat properties: `DefaultPermitLimit`,
  `DefaultWindowSeconds`, `StrictPermitLimit`, `StrictWindowSeconds`, `RelaxedPermitLimit`, `RelaxedWindowSeconds`
- Retain the existing shared settings: `SegmentsPerWindow`, `ExemptPaths`
- Define the three policy names (`default`, `strict`, `relaxed`) as a closed enum or constant set (not
  configuration-defined)
- Update the `[RateLimit("policyName")]` attribute resolution to resolve against the fixed policy set
- Add startup validation: unknown or misspelled policy names in `[RateLimit(...)]` attributes fail at startup with a
  clear error
- Update all `appsettings*.json` files to the new flat structure
- Update the rate-limiting setup extension to read the new flat properties and configure the sliding-window limiter
  accordingly
- Update affected unit tests in `emc.camus.api.test` (or the rate-limiting test project, whichever holds them
  post-US-01) to cover the new settings shape and startup validation
- Maintain identical runtime behavior: same permit limits, same windows, same 429 responses, same headers

### Out of Scope

- Adding new rate-limiting policies beyond the existing three (default, strict, relaxed)
- Changing the sliding-window algorithm or partition logic
- Changing HTTP response headers or status codes
- Modifying the cache port integration (covered in US-01)
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: `RateLimitingSettings` class exposes `DefaultPermitLimit` (int), `DefaultWindowSeconds` (int),
  `StrictPermitLimit` (int), `StrictWindowSeconds` (int), `RelaxedPermitLimit` (int), `RelaxedWindowSeconds` (int),
  `SegmentsPerWindow` (int), and `ExemptPaths` (string[])
- FR-02: The `Policies` dictionary property is removed from `RateLimitingSettings`
- FR-03: A closed set (enum or static constants) defines the valid policy names: `default`, `strict`, `relaxed`
- FR-04: The `[RateLimit("policyName")]` attribute resolves the policy name against the closed set; unknown names cause
  a startup failure with a descriptive error message
- FR-05: The rate-limiting setup extension reads the flat properties and configures three sliding-window rate limiters
  with the corresponding limits and windows
- FR-06: All `appsettings*.json` files use the new flat structure under the `RateLimitingSettings` section

### Non-Functional Requirements

- Security: No change — rate limiting continues protecting endpoints against abuse with identical limits
- Performance: No performance considerations
- Observability: Existing rate-limiting metrics remain functional; policy names in metric dimensions remain `default`,
  `strict`, `relaxed`
- Reliability: Startup validation catches misconfiguration immediately rather than at first request time
- Compliance: N/A

### Acceptance Criteria

- AC-01: `RateLimitingSettings` has no `Policies` dictionary property
- AC-02: `appsettings.Development.json` contains flat keys (`DefaultPermitLimit: 250`, `DefaultWindowSeconds: 60`,
  `StrictPermitLimit: 50`, `StrictWindowSeconds: 60`, `RelaxedPermitLimit: 500`, `RelaxedWindowSeconds: 60`,
  `SegmentsPerWindow`, `ExemptPaths`)
- AC-03: A controller decorated with `[RateLimit("strict")]` is rate-limited at the strict policy values
- AC-04: A controller decorated with `[RateLimit("unknown")]` causes the application to fail at startup with a
  descriptive error
- AC-05: HTTP responses for rate-limited requests return identical headers and status code 429 as before the change
- AC-06: The closed policy set is defined as an enum or constant class (not derived from configuration keys)

### Constraints and Dependencies

- Business constraints:
  - Must be delivered after US-01 (depends on the renamed `RateLimitingSettings` class and API-layer location)
  - Independently deployable after US-01; does not block or depend on US-02–US-05
- Dependencies:
  - US-01 completed (rate-limiting feature already relocated to API layer with `RateLimitingSettings` class)
  - Application-layer `[RateLimit]` attribute contract may need adjustment to reference the closed policy set

### Risks and Open Questions

- Risks:
  - The `[RateLimit]` attribute lives in the Application layer — referencing an API-layer enum may require introducing a
    shared constant or moving the policy names to the Application layer; owner: Tech Lead
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
  - Change summary: [What changes in domain behavior]
  - Potential files/folders to touch: `[src/Domain/... ]`
- Application
  - Change summary: [What changes in use cases | contracts]
  - Potential files/folders to touch: `[src/Application/... ]`
- API
  - Change summary: [What changes in HTTP surface]
  - Backward compatibility: `[Backward compatible | Breaking]`
  - Potential files/folders to touch: `[src/Api/... ]`
- Adapters
  - Change summary: [What changes in implementations | integrations]
  - Potential files/folders to touch: `[src/Adapters/... ]`
- Database Schema
  - Change summary: [What migrations, table, or index changes are required]
  - Potential files/folders to touch: `[src/Infrastructure/database/migrations/... ]`
- Tests
  - Change summary: [What new or updated tests are required]
  - Potential files/folders to touch: `[src/Test/... ]`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- [NFR category]: [Design decision and implementation approach]

### Delivery and Rollout Notes

- Rollout strategy: [Phased | flagged | full rollout approach]
- Rollback strategy: [How to revert safely and quickly]
- Operational readiness checks: [Monitoring, alerts, runbook updates]

### Architect Handoff Readiness

- Layer impacts are fully mapped: `[Yes | No]`
- Port | contract impacts assessed: `[Yes | No]`
- Backward compatibility decision documented: `[Yes | No]`
- Cross-cutting concern decisions addressed: `[Yes | No]`
- Rollout and rollback strategies defined: `[Yes | No]`
- Ready for implementation: `[Yes | No]`
- Architect sign-off: `[Name, Date]`

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
