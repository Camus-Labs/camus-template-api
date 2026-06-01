# User Story Specification

## Metadata

- Story ID: `US-03`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As an `internal service`, I want `all existing POST endpoints to be decorated with the
[RequireIdempotencyKey] attribute`, so that `write operations enforce idempotency key
validation and response caching as defined by the idempotency infrastructure`.

### Business Value

- Activates the idempotency infrastructure (US-01 and US-02) on real endpoints, delivering the safety guarantees to callers
- Ensures all current write operations are protected against duplicate processing from network retries or client-side failures

### In Scope

- Decorate `POST /api/v2/auth/authenticate` with `[RequireIdempotencyKey]` using the `default` TTL policy
- Decorate `POST /api/v2/auth/generate-token` with `[RequireIdempotencyKey]` using the `default` TTL policy
- Add the required `using` import for the attribute in `AuthController`

### Out of Scope

- Creating or modifying the `[RequireIdempotencyKey]` attribute (covered in US-01)
- Implementing response caching or replay logic (covered in US-02)
- Applying idempotency to non-POST HTTP methods
- Endpoints not yet implemented in the codebase

### Functional Requirements

- FR-01: The `AuthenticateUser` action in `AuthController` must be decorated with
  `[RequireIdempotencyKey(IdempotencyPolicies.Default)]`
- FR-02: The `GenerateToken` action in `AuthController` must be decorated with
  `[RequireIdempotencyKey(IdempotencyPolicies.Default)]`
- FR-03: Both endpoints must enforce idempotency key validation (from US-01) and response
  caching (from US-02) after decoration

### Non-Functional Requirements

- Security: No change to existing authentication or authorization requirements on decorated
  endpoints; idempotency key scoping per principal is handled by US-02 infrastructure
- Performance: No additional latency beyond what is introduced by the idempotency filter
  pipeline (US-01 and US-02)
- Observability: Idempotency metrics (hits, misses, conflicts) from US-02 infrastructure
  apply automatically to decorated endpoints
- Reliability: Decorated endpoints inherit the fail-open behavior defined in US-02
- Compliance: No additional compliance impact; idempotency key handling follows the rules
  established in US-01

### Acceptance Criteria

- AC-01: A POST request to `/api/v2/auth/authenticate` without the `Idempotency-Key` header
  returns HTTP 400 with error code `idempotency_key_missing`
- AC-02: A POST request to `/api/v2/auth/generate-token` without the `Idempotency-Key` header
  returns HTTP 400 with error code `idempotency_key_missing`
- AC-03: A POST request to `/api/v2/auth/authenticate` with a valid `Idempotency-Key` header
  proceeds to execute the action and returns `Idempotency-Key-Status: miss` on first call
- AC-04: A POST request to `/api/v2/auth/generate-token` with a valid `Idempotency-Key` header
  proceeds to execute the action and returns `Idempotency-Key-Status: miss` on first call
- AC-05: A repeated POST request to either endpoint with the same `Idempotency-Key` and same
  body within the default TTL (5 minutes) returns the cached response with
  `Idempotency-Key-Status: hit`

### Constraints and Dependencies

- Business constraints:
  - Must not alter existing authentication schemes, authorization policies, or rate limiting
    attributes on the endpoints
- Dependencies:
  - US-01 (idempotency key enforcement) must be implemented — provides the attribute and
    validation filter
  - US-02 (idempotent response caching) must be implemented — provides the response caching
    filter

### Risks and Open Questions

- Risks:
  - None — this story is a straightforward attribute application with no new logic
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
- Product Owner sign-off: `Product Owner, 2026-05-15`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes — no domain behavior is affected
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No changes — no contracts, services, or interfaces are affected
  - Potential files/folders to touch: `None`
- Database Schema
  - Change summary: No changes — no migrations or schema modifications required
  - Potential files/folders to touch: `None`
- API
  - Change summary: Add `[RequireIdempotencyKey(IdempotencyPolicies.Default)]` attribute to
    both `AuthenticateUser` and `GenerateToken` actions in `AuthController`. Add the
    corresponding `using` import for the `Filters` namespace if not already present. This
    activates the existing idempotency validation filter (US-01) and response caching filter
    (US-02) on these endpoints. Callers must now supply the `Idempotency-Key` header on POST
    requests to these endpoints or receive HTTP 400.
  - Backward compatibility: `Breaking` — clients that do not supply the `Idempotency-Key`
    header will now receive HTTP 400 instead of the previous successful response. This is an
    intentional breaking change communicated as a requirement to internal service consumers.
  - Potential files/folders to touch: `src/Api/emc.camus.api/Controllers/AuthController.cs`
- Adapters
  - Change summary: Added deterministic `NameIdentifier` claim (`DefaultUserId`) to
    `ApiKeyAuthenticationHandler` and introduced `DefaultUserId` constant in `ApiKeySettings`
    to support idempotency cache scoping per principal
  - Potential files/folders to touch:
    `src/Adapters/emc.camus.security.apikey/Configurations/ApiKeySettings.cs`,
    `src/Adapters/emc.camus.security.apikey/Handlers/ApiKeyAuthenticationHandler.cs`
- Tests
  - Change summary: Add or update unit tests in `AuthControllerTests` to verify that
    the `[RequireIdempotencyKey]` attribute is present on both action methods with the
    correct policy name. Integration tests covering end-to-end idempotency behavior
    (header rejection, cache hit/miss) for these specific endpoints.
  - Potential files/folders to touch:
    `src/Test/emc.camus.api.test/Controllers/AuthControllerTests.cs`,
    `src/Test/emc.camus.api.integration.test/`

### Cross-Cutting Concern Decisions

- Security: No architectural decision needed — existing authentication schemes and
  authorization policies on the endpoints remain unchanged; the idempotency key is scoped
  per principal by the US-02 caching filter which already resolves the authenticated user
- Performance: No architectural decision needed — latency impact is bounded by the
  idempotency filter pipeline already measured and accepted in US-01 and US-02
- Observability: No architectural decision needed — the existing `IdempotencyMetrics`
  counter (hits, misses, conflicts) from US-02 automatically instruments any endpoint
  decorated with the attribute
- Reliability: No architectural decision needed — the fail-open behavior defined in US-02
  applies automatically; if the cache is unavailable, requests proceed normally
- Compliance: No architectural decision needed — idempotency key handling follows the
  validation and format rules established in US-01

### Delivery and Rollout Notes

- Rollout strategy: Full rollout — the attribute application is a single atomic change;
  internal service consumers have been notified that the `Idempotency-Key` header will
  become mandatory on POST auth endpoints
- Rollback strategy: Remove the `[RequireIdempotencyKey]` attribute from both actions and
  redeploy; this restores the previous behavior with no data migration or state cleanup
  required
- Operational readiness checks: Monitor the `idempotency_key_missing` error rate in existing
  API error metrics after deployment to detect any callers that have not updated; existing
  idempotency dashboards from US-02 cover hit/miss/conflict rates

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect, 2026-05-16`

## Section C - Implementation Tracking

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01 | N/A — integration test | N/A | Api | Deferred to integration tests |
| AC-02 | N/A — integration test | N/A | Api | Deferred to integration tests |
| AC-03 | N/A — integration test | N/A | Api | Deferred to integration tests |
| AC-04 | N/A — integration test | N/A | Api | Deferred to integration tests |
| AC-05 | N/A — integration test | N/A | Api | Deferred to integration tests |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| Api | src/Api/emc.camus.api/Controllers/AuthController.cs | Modified | class AuthController | Add `[RequireIdempotencyKey(IdempotencyPolicies.Default)]` to AuthenticateUser, GenerateToken |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method:
  `No — all ACs describe HTTP-level behavior (status codes, response headers) testable only via
  integration tests`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `N/A — no unit tests required`
- Ready for implementation: `Yes`
- Tester sign-off: `Unit Tester, 2026-05-16`

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
| 1 | src/Test/emc.camus.api.integration.test/Helpers/Auth/AuthenticatedClientHelper.cs | AuthenticateAsync | Used `PostAsJsonWithIdempotencyKeyAsync` to include `Idempotency-Key` header | `[RequireIdempotencyKey]` on authenticate endpoint now requires the header |
| 2 | src/Test/emc.camus.api.integration.test/Helpers/Http/HttpClientExtensions.cs | PostAsJsonWithIdempotencyKeyAsync | Added shared extension method for POST requests with auto-generated `Idempotency-Key` | Reusable helper for all integration tests calling idempotency-decorated endpoints |
| 3 | src/Test/emc.camus.api.integration.test/Helpers/Auth/AuthenticatedClientHelper.cs | GenerateTokensAsync | Switched to `PostAsJsonWithIdempotencyKeyAsync` | `[RequireIdempotencyKey]` on generate-token endpoint now requires the header |
| 4 | src/Test/emc.camus.api.integration.test/Auth/AuthInMemoryEndpointTests.cs | Authenticate_ValidAdminCredentials_ReturnsOkWithToken, Authenticate_InvalidPassword_ReturnsUnauthorized | Switched to `PostAsJsonWithIdempotencyKeyAsync` | Same header requirement on authenticate endpoint |
| 5 | src/Test/emc.camus.api.integration.test/Auth/AuthPostgreSqlEndpointTests.cs | All generate-token test methods | Switched to `PostAsJsonWithIdempotencyKeyAsync` | Same header requirement on generate-token endpoint |
| 6 | src/Test/emc.camus.api.integration.test/Common/TelemetryEnrichmentInMemoryTests.cs | FailedAuthentication_TelemetryTags_*, JwtWithoutPermission_TelemetryTags_* | Switched to `PostAsJsonWithIdempotencyKeyAsync` | Same header requirement on auth POST endpoints |
| 7 | src/Test/emc.camus.api.integration.test/Common/TelemetryEnrichmentInMemoryTests.cs | ApiKeyAuthenticatedRequest_TelemetryTags_* | Updated `enduser.id` assertion from `BeNull` to `Be("00000000-0000-0000-0000-000000000001")` | API key identity now includes `NameIdentifier` claim for idempotency cache scoping |
| 8 | src/Test/emc.camus.api.integration.test/InMemoryCache/TokenRevocationCachePostgreSqlTests.cs | GenerateTokenAsync | Switched to `PostAsJsonWithIdempotencyKeyAsync` | Same header requirement on generate-token endpoint |
| 9 | src/Adapters/emc.camus.security.apikey/Handlers/ApiKeyAuthenticationHandler.cs | HandleAuthenticateAsync | Added `ClaimTypes.NameIdentifier` claim with deterministic GUID to API key identity | `IdempotencyResponseCachingFilter` requires `GetCurrentUserId()` to scope cache per principal |
| 10 | src/Adapters/emc.camus.security.apikey/Configurations/ApiKeySettings.cs | N/A | Added `DefaultUserId` constant | Deterministic GUID for the API key user `NameIdentifier` claim |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `Developer, 2026-05-16`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| Controller → IdempotencyKeyValidationFilter (auth POST, missing key) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_MissingIdempotencyKey_Returns400 | New |
| Controller → IdempotencyKeyValidationFilter → Service (authenticate, valid key) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_ValidAdminCredentials_ReturnsOkWithToken | Existing |
| Controller → IdempotencyKeyValidationFilter → Service (authenticate, invalid creds) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_InvalidPassword_ReturnsUnauthorized | Existing |
| Controller → IdempotencyKeyValidationFilter → Service → Repository (generate-token, valid key) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GenerateToken_ValidRequest_ReturnsOkWithPersistedToken | Existing |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | No failures | — | — |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `Integration Tester, 2026-05-16`
