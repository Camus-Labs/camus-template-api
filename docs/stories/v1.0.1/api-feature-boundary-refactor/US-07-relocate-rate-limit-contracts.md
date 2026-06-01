# User Story Specification

## Metadata

- Story ID: `US-07`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `relocate-rate-limit-contracts`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the RateLimitAttribute and RateLimitPolicies relocated from Application/RateLimiting/ into the API
layer`, so that `rate-limiting contracts live alongside their sole consumers (controllers and
middleware) and no orphaned cross-layer contracts remain after US-01`.

### Business Value

- Eliminates dead contracts in the Application layer — after US-01, rate limiting is fully an API-layer concern with no
  adapter and no port
- Maintains architectural discipline: contracts live where they are consumed, not in a shared layer with no external
  consumers
- Follows the established pattern where `RequireIdempotencyKeyAttribute` and `IdempotencyPolicies` live in the API
  project alongside the idempotency filter

### In Scope

- Move `RateLimitAttribute.cs` from `src/Application/emc.camus.application/RateLimiting/` into
  `src/Api/emc.camus.api/Filters/`
- Move `RateLimitPolicies.cs` from `src/Application/emc.camus.application/RateLimiting/` into
  `src/Api/emc.camus.api/Configurations/`
- Update namespaces from `emc.camus.application.RateLimiting` to the appropriate API-layer namespaces
  (`emc.camus.api.Filters`, `emc.camus.api.Configurations`)
- Update all `using` statements in controllers, middleware, and setup extensions that reference the old namespace
- Remove the `RateLimiting/` folder from Application if nothing else remains in it
- Maintain identical attribute behavior and policy constant values

### Out of Scope

- Idempotency contracts (they still follow the port/adapter pattern with cache port consumers)
- Behavioral changes to the attribute or policy constants
- Changing the `[RateLimit]` attribute resolution logic (covered in US-06)
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: `RateLimitAttribute.cs` exists in `src/Api/emc.camus.api/Filters/` with namespace `emc.camus.api.Filters`
- FR-02: `RateLimitPolicies.cs` exists in `src/Api/emc.camus.api/Configurations/` with namespace
  `emc.camus.api.Configurations`
- FR-03: The `src/Application/emc.camus.application/RateLimiting/` folder is removed (or emptied of rate-limiting types)
- FR-04: All controllers and middleware referencing `RateLimitAttribute` or `RateLimitPolicies` compile with updated
  `using` statements
- FR-05: No Application-layer source files reference rate-limiting types after relocation

### Non-Functional Requirements

- Security: No change — rate limiting attribute continues decorating endpoints identically
- Performance: No performance considerations
- Observability: N/A (no metrics changes)
- Reliability: No intermediate broken state; solution builds after relocation
- Compliance: N/A

### Acceptance Criteria

- AC-01: Solution builds successfully with `RateLimitAttribute` and `RateLimitPolicies` in the API layer
- AC-02: No source files under `src/Application/` contain references to `RateLimitAttribute` or `RateLimitPolicies`
- AC-03: The `src/Application/emc.camus.application/RateLimiting/` folder does not exist (or contains no rate-limiting
  types)
- AC-04: Controllers decorated with `[RateLimit(...)]` compile and resolve against the API-layer namespace
- AC-05: `RateLimitPolicies` constant values remain identical (`default`, `strict`, `relaxed`)

### Constraints and Dependencies

- Business constraints:
  - Must be delivered after US-01 (rate-limiting feature must already be in the API layer)
  - Sequenced before US-05 (test consolidation) and before US-06 (settings flattening references the policy constants)
- Dependencies:
  - US-01 completed (rate-limiting feature relocated to API layer)

### Risks and Open Questions

- Risks:
  - If other Application-layer types reference `RateLimitPolicies` or `RateLimitAttribute`, those references must be
    updated or removed — mitigation: search for usages before moving; owner: Tech Lead
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
- Product Owner sign-off: `3M0R4C, 2026-05-27`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- Application
  - Change summary: Delete `RateLimitAttribute.cs` and `RateLimitPolicies.cs` from the `RateLimiting/` namespace. Remove
    the `RateLimiting/` folder entirely if empty after deletion. No other Application-layer types reference these two
    files (verified — all consumers are controllers and the rate-limiting setup extension, both in the API layer
    post-US-01).
  - Potential files/folders to touch: `src/Application/emc.camus.application/RateLimiting/` (deletion)
- Database Schema
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- API
  - Change summary: Receive `RateLimitAttribute.cs` into `Filters/` (namespace `emc.camus.api.Filters`) and
    `RateLimitPolicies.cs` into `Configurations/` (namespace `emc.camus.api.Configurations`). Update `using` statements
    in controllers (`AuthController`, `ApiInfoController`) and the rate-limiting setup extension to reference the new
    namespaces. No HTTP surface change — attribute behavior and policy constant values are identical.
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch: `src/Api/emc.camus.api/Filters/RateLimitAttribute.cs` (new),
    `src/Api/emc.camus.api/Configurations/RateLimitPolicies.cs` (new),
    `src/Api/emc.camus.api/Controllers/AuthController.cs`, `src/Api/emc.camus.api/Controllers/ApiInfoController.cs`,
    `src/Api/emc.camus.api/Extensions/RateLimitingSetupExtensions.cs`,
    `src/Api/emc.camus.api/Configurations/RateLimitingSettings.cs`
- Adapters
  - Change summary: No changes — by the time US-07 executes, US-01 has already removed `emc.camus.ratelimiting.inmemory`
    and all its references to these types now live in the API layer
  - Potential files/folders to touch: `None`
- Tests
  - Change summary: Out of scope (deferred to US-05). Any unit tests referencing the old namespace will be updated in
    the test consolidation story.
  - Potential files/folders to touch: `None (deferred to US-05)`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No change — `[RateLimit]` attribute continues to decorate endpoints identically. Only the namespace changes;
  the `AttributeUsage` targets, `PolicyName` property, and policy constant values remain untouched.
- Performance: No runtime impact — attribute resolution uses the same `GetMetadata<T>()` mechanism regardless of which
  assembly the type lives in.
- Observability: N/A — no metrics involved in the attribute or constants.
- Reliability: Single-commit relocation. Solution must build cleanly with zero references to the old
  `emc.camus.application.RateLimiting` namespace in source files.

### Delivery and Rollout Notes

- Rollout strategy: Full rollout in a single commit. No feature flag needed — pure namespace relocation with zero
  runtime behavior change.
- Rollback strategy: Revert the commit. The previous Application-layer files are restored by git history.
- Operational readiness checks: Build verification only. No runtime observability changes to validate.

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `3M0R4C, 2026-05-27`

## Section C - Implementation Tracking

### Test Traceability

| AC    | Test Class                | Test Method                                           | Layer | Change |
| ----- | ------------------------- | ----------------------------------------------------- | ----- | ------ |
| AC-01 | RateLimitAttributeTests   | Constructor_ValidPolicyName_SetsProperty              | Api   | New    |
| AC-01 | RateLimitAttributeTests   | Class_ResolvesFromApiFiltersNamespace                 | Api   | New    |
| AC-02 | RateLimitAttributeTests   | Class_ResolvesFromApiFiltersNamespace                 | Api   | New    |
| AC-03 | —                         | (verified by deletion — no test needed)               | —     | —      |
| AC-04 | RateLimitAttributeTests   | Class_HasAttributeUsage_AllowsClassAndMethodTargets   | Api   | New    |
| AC-04 | RateLimitAttributeTests   | Class_HasAttributeUsage_DisallowsMultiple             | Api   | New    |
| AC-04 | RateLimitAttributeTests   | Class_HasAttributeUsage_IsInherited                   | Api   | New    |
| AC-04 | RateLimitAttributeTests   | Constructor_InvalidPolicyName_ThrowsArgumentException | Api   | New    |
| AC-05 | RateLimitingSettingsTests | (existing — uses RateLimitPolicies constants)         | Api   | —      |

### Skeleton Inventory

| Layer | Stub File                                                       | Change   | Types                          | Members                                                               |
| ----- | --------------------------------------------------------------- | -------- | ------------------------------ | --------------------------------------------------------------------- |
| Api   | src/Api/emc.camus.api/Filters/RateLimitAttribute.cs             | New      | class RateLimitAttribute       | `string PolicyName { get; }`, `RateLimitAttribute(string policyName)` |
| Api   | src/Api/emc.camus.api/Configurations/RateLimitPolicies.cs       | New      | static class RateLimitPolicies | `const Default`, `const Strict`, `const Relaxed`, `string[] GetAll()` |
| Api   | src/Api/emc.camus.api/Controllers/AuthController.cs             | Modified | —                              | Updated `using` → `emc.camus.api.Filters`                             |
| Api   | src/Api/emc.camus.api/Controllers/ApiInfoController.cs          | Modified | —                              | Updated `using` → `emc.camus.api.Filters`                             |
| Api   | src/Api/emc.camus.api/Extensions/RateLimitingSetupExtensions.cs | Modified | —                              | Added `using emc.camus.api.Filters`                                   |
| Api   | src/Api/emc.camus.api/Configurations/RateLimitingSettings.cs    | Modified | —                              | Removed `using emc.camus.application.RateLimiting`                    |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `3M0R4C, 2026-05-28`

### Regression Fixes Log

| #   | Test File                                                                        | Test Method | Change Made                                                                               | Reason                                                                                        |
| --- | -------------------------------------------------------------------------------- | ----------- | ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| 1   | src/Test/emc.camus.api.test/Configurations/RateLimitingSettingsTests.cs          | (all)       | Removed `using emc.camus.application.RateLimiting`                                        | Ambiguous reference with new `emc.camus.api.Configurations.RateLimitPolicies`                 |
| 2   | src/Test/emc.camus.api.integration.test/Common/MiddlewareHeadersInMemoryTests.cs | (all)       | Changed `using emc.camus.application.RateLimiting` → `using emc.camus.api.Configurations` | Namespace no longer exists after type relocation                                              |
| 3   | src/Test/emc.camus.api.integration.test/Common/RateLimitingIpPartitionTests.cs   | (all)       | Changed `using emc.camus.application.RateLimiting` → `using emc.camus.api.Configurations` | Namespace no longer exists after type relocation                                              |
| 4   | src/Test/emc.camus.application.test/RateLimiting/RateLimitAttributeTests.cs      | (all)       | Deleted file and folder                                                                   | Tests orphaned type that was relocated to API layer (new tests exist in `emc.camus.api.test`) |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-05-28`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary                                                   | Factory                | Test Class                     | Test Method                                                                                   | Change   |
| ---------------------------------------------------------- | ---------------------- | ------------------------------ | --------------------------------------------------------------------------------------------- | -------- |
| HTTP → Controller `[RateLimit]` → Rate Limiting Middleware | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | SameIp_ExceedsPermitLimit_Returns429WithErrorCodeAndHeaders                                   | Existing |
| HTTP → Controller `[RateLimit]` → Rate Limiting Middleware | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | DifferentIps_SameEndpoint_HaveIndependentRateLimitBuckets                                     | Existing |
| HTTP → Controller `[RateLimit]` → Rate Limiting Middleware | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | ExhaustedIp_SameEndpoint_RemainsThrottled                                                     | Existing |
| HTTP → Middleware pipeline → RateLimitPolicies constants   | ApiInMemoryFactory     | MiddlewareHeadersInMemoryTests | AnonymousRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAnonymousUsername            | Existing |
| HTTP → Middleware pipeline → RateLimitPolicies constants   | ApiInMemoryFactory     | MiddlewareHeadersInMemoryTests | AuthenticatedJwtRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAuthenticatedUsername | Existing |

### Integration Test Findings

| #   | Test | Failure | Root Cause Analysis | Affected File |
| --- | ---- | ------- | ------------------- | ------------- |
| —   | —    | —       | No failures         | —             |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `3M0R4C, 2026-05-28`
