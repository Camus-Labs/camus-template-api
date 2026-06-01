# User Story Specification

## Metadata

- Story ID: `US-06`
- Owner: `3M0R4C`
- Status: `Done`

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
    shared constant or moving the policy names to the Application layer; owner: 3M0R4C
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
  - Change summary: No changes — rate limiting is not a domain concern
  - Potential files/folders to touch: `None`
- Application
  - Change summary: Update the Application README documentation to reflect that `RateLimitPolicies` now lives in the
    API layer (documentation-only; no source code changes since the constants and attribute already reside in the API
    layer post-US-01)
  - Potential files/folders to touch: `src/Application/emc.camus.application/README.md`
- Database Schema
  - Change summary: No changes — rate limiting is stateless and in-memory
  - Potential files/folders to touch: `None`
- API
  - Change summary: Restructure `RateLimitingSettings` from a `Policies` dictionary to flat properties
    (`DefaultPermitLimit`, `DefaultWindowSeconds`, `StrictPermitLimit`, `StrictWindowSeconds`,
    `RelaxedPermitLimit`, `RelaxedWindowSeconds`) while retaining `SegmentsPerWindow` and `ExemptPaths`.
    Remove `RateLimitPolicySettings` nested class. Update `Validate()` to validate the flat properties directly.
    Update `RateLimitingSetupExtensions` to read flat properties and configure three sliding-window limiters.
    Add startup validation in the setup extension that scans all `[RateLimit]` attribute usages and fails if any
    policy name is not in the `RateLimitPolicies.GetAll()` set. Update `appsettings.json` and
    `appsettings.Development.json` to the new flat structure.
  - Backward compatibility: `Backward compatible` — no HTTP surface change; internal configuration restructure only;
    runtime behavior (429 responses, headers, limits) remains identical
  - Potential files/folders to touch: `src/Api/emc.camus.api/Configurations/RateLimitingSettings.cs`,
    `src/Api/emc.camus.api/Extensions/RateLimitingSetupExtensions.cs`,
    `src/Api/emc.camus.api/appsettings.json`,
    `src/Api/emc.camus.api/appsettings.Development.json`
- Adapters
  - Change summary: No changes — the in-memory rate-limiting adapter (`emc.camus.ratelimiting.inmemory`) is
    deprecated post-US-01 since the rate-limiting feature was relocated to the API layer. Its
    `InMemoryRateLimitingSettings` class is independent and unaffected.
  - Potential files/folders to touch: `None`
- Tests
  - Change summary: Update existing `RateLimitingSettingsTests` to cover the new flat property shape and updated
    `Validate()` behavior. Add tests for startup validation of `[RateLimit]` attribute policy names against the
    closed set. Remove tests that exercise the `Policies` dictionary structure.
  - Potential files/folders to touch: `src/Test/emc.camus.api.test/Configurations/RateLimitingSettingsTests.cs`,
    `src/Test/emc.camus.api.test/Extensions/RateLimitingSetupExtensionsTests.cs`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No decision needed — rate-limiting enforcement remains identical; the flat structure does not alter
  permit limits, window durations, or endpoint protection behavior
- Performance: No decision needed — the flat property model eliminates dictionary lookups at configuration-read time,
  which is negligible but not a regression
- Observability: Existing rate-limiting metrics and metric dimensions (`default`, `strict`, `relaxed`) remain
  unchanged because the policy name constants in `RateLimitPolicies` are unmodified
- Reliability: Startup validation is strengthened by failing immediately when a `[RateLimit]` attribute references
  a policy name not present in the closed `RateLimitPolicies.GetAll()` set, catching misconfiguration before
  serving traffic

### Delivery and Rollout Notes

- Rollout strategy: Full rollout — this is an internal configuration restructure with identical runtime behavior;
  deploy as a single commit after US-01 is merged
- Rollback strategy: Revert the commit and restore the `Policies` dictionary shape in `appsettings*.json`; no data
  migration or state cleanup required
- Operational readiness checks: Verify rate-limiting metrics emit after deployment; confirm 429 responses on load
  test with same thresholds as before; no new alerts or runbook updates needed

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `3M0R4C, 2026-05-28`

## Section C - Implementation Tracking

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01 | RateLimitingSettingsTests | RateLimitingSettings_DoesNotHavePoliciesDictionaryProperty | Api | New |
| AC-02 | RateLimitingSettingsTests | DefaultSettings_HasExpectedFlatPropertyDefaults | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_ValidSettings_DoesNotThrow | Api | Modified |
| AC-03 | RateLimitingSettingsTests | Validate_DefaultPermitLimitInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_StrictPermitLimitInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_RelaxedPermitLimitInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_DefaultWindowSecondsInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_StrictWindowSecondsInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_RelaxedWindowSecondsInvalid_ThrowsInvalidOperationException | Api | New |
| AC-03 | RateLimitingSettingsTests | Validate_ValidSegmentsPerWindow_DoesNotThrow | Api | Modified |
| AC-03 | RateLimitingSettingsTests | Validate_SegmentsPerWindowOutOfRange_ThrowsInvalidOperationException | Api | Modified |
| AC-04 | RateLimitAttributeTests | Constructor_UnrecognizedPolicyName_ThrowsArgumentException | Api | New |
| AC-04 | RateLimitAttributeTests | Constructor_RecognizedPolicyName_SetsProperty | Api | New |
| AC-05 | RateLimitingSettingsTests | Validate_NullExemptPaths_ThrowsInvalidOperationException | Api | Modified |
| AC-05 | RateLimitingSettingsTests | Validate_ExemptPathWithInvalidEntry_ThrowsInvalidOperationException | Api | Modified |
| AC-05 | RateLimitingSettingsTests | Validate_ExemptPathWithoutLeadingSlash_ThrowsInvalidOperationException | Api | Modified |
| AC-05 | RateLimitingSettingsTests | Validate_EmptyExemptPaths_DoesNotThrow | Api | Modified |
| AC-06 | RateLimitAttributeTests | Constructor_UnrecognizedPolicyName_ThrowsArgumentException | Api | New |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| Api | src/Api/emc.camus.api/Configurations/RateLimitingSettings.cs | Modified | sealed class RateLimitingSettings | DefaultPermitLimit, DefaultWindowSeconds, StrictPermitLimit, StrictWindowSeconds, RelaxedPermitLimit, RelaxedWindowSeconds, SegmentsPerWindow, ExemptPaths; Validate() → throw NotImplementedException() |
| Api | src/Api/emc.camus.api/Filters/RateLimitAttribute.cs | Modified | class RateLimitAttribute | ValidatePolicyName(string) — validates against RateLimitPolicies closed set at construction |
| Api | src/Api/emc.camus.api/Extensions/RateLimitingSetupExtensions.cs | Modified | static class RateLimitingSetupExtensions | ResolvePermitLimit(settings, policyName), ResolveWindowSeconds(settings, policyName) — switch-based resolution |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `3M0R4C, 2026-05-28`

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
| 1 | src/Test/emc.camus.api.integration.test/Fixtures/ApiFactoryBase.cs | ConfigureWebHost | Updated UseSetting keys from `RateLimitingSettings:Policies:{name}:PermitLimit` to `RateLimitingSettings:{Name}PermitLimit` (flat keys) | RateLimitingSettings removed Policies dictionary in favor of flat properties |
| 2 | src/Test/emc.camus.api.integration.test/Fixtures/ApiRateLimitingFactory.cs | ConfigureVariantHostSettings | Updated UseSetting keys from `RateLimitingSettings:Policies:{name}:PermitLimit` to `RateLimitingSettings:{Name}PermitLimit` (flat keys) | RateLimitingSettings removed Policies dictionary in favor of flat properties |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-05-28`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| HTTP → Rate Limiting Pipeline → Controller (relaxed policy enforcement with flat settings) | ApiRateLimitingFactory | RateLimitingIpPartitionTests | RelaxedPolicy_ThrottlesAtRelaxedLimit_NotDefaultOrStrictLimit | Existing |
| HTTP → Rate Limiting Pipeline → Controller (default policy enforcement with flat settings) | ApiRateLimitingFactory | RateLimitingIpPartitionTests | DefaultPolicy_ThrottlesAtDefaultLimit_NotRelaxedOrStrictLimit | Existing |
| HTTP → Rate Limiting Pipeline → 429 Rejection (headers + error code with flat settings) | ApiRateLimitingFactory | RateLimitingIpPartitionTests | SameIp_ExceedsPermitLimit_Returns429WithErrorCodeAndHeaders | Existing |
| HTTP → Rate Limiting Pipeline → IP Partitioning (independent buckets with flat settings) | ApiRateLimitingFactory | RateLimitingIpPartitionTests | DifferentIps_SameEndpoint_HaveIndependentRateLimitBuckets | Existing |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | No failures | — | — |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `3M0R4C, 2026-05-28`
