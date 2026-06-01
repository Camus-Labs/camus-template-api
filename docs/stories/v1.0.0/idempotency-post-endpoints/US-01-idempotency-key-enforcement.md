# User Story Specification

## Metadata

- Story ID: `US-01`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As an `internal service`, I want `POST endpoints decorated with [RequireIdempotencyKey] to reject
requests missing the Idempotency-Key header`, so that `all write operations are explicitly identified
for duplicate detection before processing`.

### Business Value

- Prevents accidental double-processing of data write requests caused by network retries or client-side failures
- Establishes a clear contract: decorated endpoints require callers to provide an idempotency key, enabling safe retries

### In Scope

- `[RequireIdempotencyKey]` attribute definition in the API layer
- Idempotency filter/middleware that validates the `Idempotency-Key` header presence on decorated endpoints
- Configurable TTL policies (default: 5 minutes, long-term: 24 hours) defined in appsettings
- API extension method for loading idempotency services in `Program.cs`
- Return `400 Bad Request` with a descriptive error when the header is missing on a decorated endpoint

### Out of Scope

- Response caching and replay (covered in US-02)
- Body conflict detection (covered in US-02)
- Distributed/external cache backends (Redis, database)
- Idempotency for non-POST HTTP methods

### Functional Requirements

- FR-01: Define a `[RequireIdempotencyKey]` attribute in the API layer that can be applied to
  controller actions, accepting a required TTL policy name parameter
- FR-02: When a request hits an endpoint decorated with `[RequireIdempotencyKey]` and the
  `Idempotency-Key` header is absent or empty, return HTTP 400 with error code
  `idempotency_key_missing`
- FR-03: The `Idempotency-Key` header value must be a non-empty string (max 256 characters);
  invalid values return HTTP 400 with error code `idempotency_key_invalid`
- FR-04: Idempotency settings (TTL policies) must be configurable via appsettings with two
  default policies: `default` (5 minutes) and `long-term` (24 hours)
- FR-05: Provide an API extension method (`AddIdempotency()`) to register idempotency services
  and the validation filter in the DI container

### Non-Functional Requirements

- Security: Idempotency key is validated as a header presence and format check;
  per-principal key scoping is deferred to US-02
- Performance: Header validation overhead must be negligible (< 1ms added latency);
  no I/O for validation-only path
- Observability: Rejection responses include the `Trace-Id` header (provided by the
  observability middleware earlier in the pipeline)
- Reliability: The filter must not throw unhandled exceptions; graceful degradation if misconfigured
- Compliance: No PII stored in idempotency key values; keys are opaque client-generated identifiers

### Acceptance Criteria

- AC-01: A POST request to a `[RequireIdempotencyKey]`-decorated endpoint without the
  `Idempotency-Key` header returns HTTP 400 with body containing error code
  `idempotency_key_missing`
- AC-02: A POST request with an empty or whitespace-only `Idempotency-Key` header returns
  HTTP 400 with error code `idempotency_key_invalid`
- AC-03: A POST request with a valid `Idempotency-Key` header (1-256 non-empty characters)
  passes validation and proceeds to the action
- AC-04: Endpoints without the `[RequireIdempotencyKey]` attribute are unaffected by the
  idempotency filter
- AC-05: Idempotency TTL policies are loaded from appsettings and injectable; default policies
  are `default` (5 min) and `long-term` (24 hours)
- AC-06: The `AddIdempotency()` extension registers all required services and the validation filter

### Constraints and Dependencies

- Business constraints:
  - Must follow existing attribute pattern established by `[RateLimit]` in `Application/RateLimiting/`
  - Must be wired as an API extension in `Program.cs` consistent with other cross-cutting concerns
- Dependencies:
  - Existing `IUserContext` interface for extracting authenticated principal

### Risks and Open Questions

- Risks:
  - Misconfigured policy names on the attribute could silently default — mitigated by startup
    validation of policy references
- Open questions:
  - None remaining

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
- Product Owner sign-off: `Product Owner, 2026-05-02`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes — idempotency enforcement is a cross-cutting infrastructure
    concern with no domain business logic impact
  - Potential files/folders to touch: `None`
- Application
  - Change summary: Add new error code constants (`idempotency_key_missing`,
    `idempotency_key_invalid`) in `ErrorCodes` and the `Idempotency-Key` header name constant
    to the existing `Headers` class.
  - Potential files/folders to touch: `src/Application/emc.camus.application/Common/ErrorCodes.cs`,
    `src/Application/emc.camus.application/Common/Headers.cs`
- Database Schema
  - Change summary: No changes — this story uses only in-memory validation with no persistence
  - Potential files/folders to touch: `None`
- API
  - Change summary: Implement idempotency key validation as an API-layer cross-cutting concern.
    Add an `IdempotencySetupExtensions.cs` in `Extensions/` exposing `AddIdempotency()` method.
    Define `[RequireIdempotencyKey]` attribute and `IdempotencyPolicies` constants in the API
    layer. Add an action filter in `Filters/` that reads the `[RequireIdempotencyKey]` attribute
    from endpoint metadata, validates the `Idempotency-Key` header presence and format
    (non-empty, max 256 chars), and throws `ArgumentException` on violations (mapped to
    HTTP 400 by `ExceptionHandlingMiddleware`). Add `IdempotencySettings` configuration class
    in `Configurations/`. Register in `Program.cs` and add settings to `appsettings.json`.
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch:
    `src/Api/emc.camus.api/Extensions/IdempotencySetupExtensions.cs`,
    `src/Api/emc.camus.api/Filters/IdempotencyKeyValidationFilter.cs`,
    `src/Api/emc.camus.api/Filters/RequireIdempotencyKeyAttribute.cs`,
    `src/Api/emc.camus.api/Configurations/IdempotencySettings.cs`,
    `src/Api/emc.camus.api/Configurations/IdempotencyPolicies.cs`,
    `src/Api/emc.camus.api/Program.cs`,
    `src/Api/emc.camus.api/appsettings.json`
- Adapters
  - Change summary: No changes — idempotency key validation has no external infrastructure
    dependency and does not implement an Application-layer port interface, so it belongs in
    the API layer directly
  - Potential files/folders to touch: `None`
- Tests
  - Change summary: Unit tests for the attribute definition (Application layer), unit tests
    for the validation filter logic (API layer covering AC-01 through AC-04), configuration
    binding and validation tests (AC-05), and extension method registration tests (AC-06).
    Integration tests validating end-to-end HTTP 400 rejection on decorated endpoints.
  - Potential files/folders to touch:
    `src/Test/emc.camus.api.test/Filters/RequireIdempotencyKeyAttributeTests.cs`,
    `src/Test/emc.camus.api.test/Filters/IdempotencyKeyValidationFilterTests.cs`,
    `src/Test/emc.camus.api.test/Configurations/IdempotencySettingsTests.cs`,
    `src/Test/emc.camus.api.integration.test/`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: The validation filter performs header presence and format checking only.
  Per-principal key scoping is deferred to US-02 caching layer.
- Performance: Header validation is purely in-memory string inspection with no I/O, no
  allocations beyond reading the header value, and no cache lookups in this story (cache
  interactions deferred to US-02). Overhead is negligible (< 1ms).
- Observability: The filter throws `ArgumentException` on invalid/missing headers, which is
  caught by `ExceptionHandlingMiddleware` and mapped to structured error responses. The
  rejection response includes the `Trace-Id` header (already added by the observability
  middleware earlier in the pipeline).
- Reliability: The filter catches configuration errors at startup via options validation
  (fail-fast). Runtime filter logic uses no external dependencies.
- Compliance: The idempotency key is treated as an opaque string identifier. No PII validation
  or storage occurs. Key values are never logged beyond their presence/absence status.

### Delivery and Rollout Notes

- Rollout strategy: Full rollout — the `[RequireIdempotencyKey]` attribute is opt-in per
  endpoint. No existing endpoints are decorated in this story, so deploying the infrastructure
  has zero runtime impact until a controller action is explicitly annotated.
- Rollback strategy: Remove the `AddIdempotency()` call from `Program.cs` and redeploy. The
  attribute on any decorated endpoints becomes inert (no filter registered to act on it). No
  data migration or state cleanup required.
- Operational readiness checks: Verify startup validation passes with the configured policy
  names. Monitor warning logs for `idempotency_key_missing` and `idempotency_key_invalid`
  rejection events after endpoints are decorated. No new alerts required for this story;
  alerting thresholds will be established when US-02 introduces caching.

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect, 2026-05-03`

## Section C - Implementation Tracking

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01 | IdempotencyKeyValidationFilterTests | OnActionExecuting_MissingIdempotencyKeyHeader_ThrowsArgumentExceptionWithMissingMessage | Api | New |
| AC-02 | IdempotencyKeyValidationFilterTests | OnActionExecuting_InvalidIdempotencyKey_ThrowsArgumentExceptionWithInvalidMessage | Api | New |
| AC-03 | IdempotencyKeyValidationFilterTests | OnActionExecuting_ValidIdempotencyKey_DoesNotThrow | Api | New |
| AC-04 | IdempotencyKeyValidationFilterTests | OnActionExecuting_NoRequireIdempotencyKeyAttribute_DoesNotThrow | Api | New |
| AC-05 | IdempotencySettingsTests | Validate_ValidSettings_DoesNotThrow | Api | New |
| AC-05 | IdempotencySettingsTests | Validate_StandardTtlSecondsOutOfRange_ThrowsInvalidOperationException | Api | New |
| AC-05 | IdempotencySettingsTests | Validate_LongTermTtlSecondsOutOfRange_ThrowsInvalidOperationException | Api | New |
| AC-06 | RequireIdempotencyKeyAttributeTests | Constructor_ValidPolicyName_SetsProperty | Api | New |
| AC-06 | RequireIdempotencyKeyAttributeTests | Constructor_InvalidPolicyName_ThrowsArgumentException | Api | New |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| Application | src/Application/emc.camus.application/Common/ErrorCodes.cs | Modified | static class ErrorCodes | IdempotencyKeyMissing, IdempotencyKeyInvalid |
| Application | src/Application/emc.camus.application/Common/Headers.cs | Modified | static class Headers | IdempotencyKey |
| Api | src/Api/emc.camus.api/Configurations/IdempotencyPolicies.cs | New | static class IdempotencyPolicies | Default, LongTerm |
| Api | src/Api/emc.camus.api/Configurations/IdempotencySettings.cs | New | sealed class IdempotencySettings | StandardTtlSeconds, LongTermTtlSeconds, Validate() |
| Api | src/Api/emc.camus.api/Filters/RequireIdempotencyKeyAttribute.cs | New | class RequireIdempotencyKeyAttribute | PolicyName, ctor(string) |
| Api | src/Api/emc.camus.api/Filters/IdempotencyKeyValidationFilter.cs | New | class IdempotencyKeyValidationFilter | OnActionExecuting(), OnActionExecuted() |
| Api | src/Api/emc.camus.api/Extensions/IdempotencySetupExtensions.cs | New | static class IdempotencySetupExtensions | AddIdempotency() |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `Unit Tester, 2026-05-13`

### Developer Handoff Gate

- All tests pass (TDD green): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `Developer, 2026-05-13`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| HTTP POST → filter → 400 (missing header) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostToDecoratedEndpoint_MissingIdempotencyKeyHeader_Returns400WithMissingErrorCode | New |
| HTTP POST → filter → controller action (valid key) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostToDecoratedEndpoint_ValidIdempotencyKey_Returns200WithBody | New |
| HTTP POST → filter (no-op) → controller action | ApiInMemoryFactory | IdempotencyInMemoryTests | PostToUndecoratedEndpoint_NoIdempotencyKeyHeader_Returns200WithBody | New |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | No failures | — | — | — |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `Integration Tester, 2026-05-13`
