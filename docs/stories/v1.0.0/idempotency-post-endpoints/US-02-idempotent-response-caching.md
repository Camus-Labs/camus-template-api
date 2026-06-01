# User Story Specification

## Metadata

- Story ID: `US-02`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As an `internal service`, I want `duplicate POST requests with the same Idempotency-Key to return
the original cached response without re-executing the action`, so that `retried write operations are
safe and produce consistent results without double-processing`.

### Business Value

- Eliminates duplicate data writes caused by network retries, ensuring exactly-once processing semantics
- Reduces unnecessary load on downstream services and databases by replaying cached responses for repeated requests

### In Scope

- Store the response (status code, body) in the in-memory cache keyed by
  `(authenticated principal, idempotency key)`
- Replay cached response on subsequent requests with the same key within the TTL window
- Detect request body mismatch for the same idempotency key and return HTTP 409 with error code `idempotency_body_conflict`
- Respect the TTL policy specified on the `[RequireIdempotencyKey]` attribute (default or long-term)
- Include `Idempotency-Key-Status` response header indicating `hit` (cached) or `miss` (first request)

### Out of Scope

- Distributed cache backends (Redis, database-backed)
- Idempotency for GET, PUT, PATCH, DELETE methods
- Automatic cache eviction strategies beyond TTL expiry
- Idempotency key generation on behalf of the client

### Functional Requirements

- FR-01: After a successful action execution on a decorated endpoint, cache the full HTTP response
  (status code, response body) keyed by the composite key
  `{user-principal}:{idempotency-key}`
- FR-02: When a subsequent request arrives with the same composite key within the TTL window and an
  identical request body hash, return the cached response with original status code and an
  `Idempotency-Key-Status: hit` header
- FR-03: When a subsequent request arrives with the same composite key but a different request body
  hash, return HTTP 409 Conflict with error code `idempotency_body_conflict` and do not execute
  the action
- FR-04: Cache entries expire according to the TTL policy configured on the attribute; `default`
  policy defaults to 5 minutes, `long-term` defaults to 24 hours
- FR-05: First-time requests include an `Idempotency-Key-Status: miss` response header to
  distinguish from cached responses
- FR-06: Do not cache when the action throws an unhandled exception — allow the client to
  retry with the same key on transient failures

### Non-Functional Requirements

- Security: Cache entries are isolated per authenticated principal; one user cannot access another
  user's cached responses; cache keys must not leak PII
- Performance: Cache lookup must use O(1) access patterns; body hashing must use a fast algorithm
  (e.g., SHA256 of request body); total added latency for cache hit path < 5ms
- Observability: Emit metrics for cache hits, body conflicts, and cache errors
- Reliability: If the in-memory cache is unavailable or throws, fail open (process request normally
  without caching) and log a warning
- Compliance: Cached response bodies may contain sensitive data — they are stored in-process memory
  only and expire via TTL

### Acceptance Criteria

- AC-01: A POST request with a valid `Idempotency-Key` to a decorated endpoint executes the
  action, caches the response, and returns `Idempotency-Key-Status: miss`
- AC-02: A repeated POST request with the same `Idempotency-Key`, same user, and same request
  body within TTL returns the cached response with original status code and
  `Idempotency-Key-Status: hit` without re-executing the action
- AC-03: A repeated POST request with the same `Idempotency-Key` and same user but a different
  request body returns HTTP 409 with error code `idempotency_body_conflict`
- AC-04: A cached entry expires after the configured TTL; subsequent requests with the same key
  after expiry are treated as new requests
- AC-05: Two different authenticated users using the same `Idempotency-Key` value are treated
  independently (no cross-user cache collision)
- AC-06: If the action returns a non-success response (4xx or 5xx), the response is not cached
  and the same key can be retried
- AC-07: If the in-memory cache is unavailable, the request proceeds normally without idempotency caching (fail-open behavior)

### Constraints and Dependencies

- Business constraints:
  - Must use in-process memory storage via `ConcurrentDictionary` (no external dependencies)
  - Must integrate with the idempotency extension registered in US-01
- Dependencies:
  - US-01 (idempotency key enforcement) must be implemented first — this story extends the filter/middleware
  - `IUserContext` for authenticated principal extraction
  - `ConcurrentDictionary` from `System.Collections.Concurrent`

### Risks and Open Questions

- Risks:
  - Memory pressure from large cached response bodies — mitigated by TTL expiry and configurable
    size limits if needed in future
  - Concurrent duplicate requests arriving simultaneously before first completes — mitigated by
    using cache entry locking (e.g., `SemaphoreSlim` or `GetOrCreate` pattern)
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
  - Change summary: No changes — this feature is entirely a cross-cutting HTTP concern with no domain logic
  - Potential files/folders to touch: `None`
- Application
  - Change summary: Define a new `IIdempotencyResponseCache` port interface in the Application layer
    with methods for storing, retrieving, and checking cached idempotent responses (keyed by composite
    key, storing status code, body, and body hash with TTL); add new
    `IdempotencyBodyConflict` error code constant to `ErrorCodes`; add `IdempotencyKeyStatus` header
    constant to `Headers`
  - Potential files/folders to touch:
    `src/Application/emc.camus.application/Idempotency/IIdempotencyResponseCache.cs`,
    `src/Application/emc.camus.application/Idempotency/CachedResponse.cs`,
    `src/Application/emc.camus.application/Common/ErrorCodes.cs`,
    `src/Application/emc.camus.application/Common/Headers.cs`
- Database Schema
  - Change summary: No changes — storage is in-process memory only via the cache adapter
  - Potential files/folders to touch: `None`
- API
  - Change summary: Introduce a new `IdempotencyResponseCachingFilter` (resource filter or result
    filter) that wraps action execution — on cache miss it hashes the request body, executes the
    action, stores the response via `IIdempotencyResponseCache`, and appends
    `Idempotency-Key-Status: miss`; on cache hit with matching body hash it short-circuits with the
    cached response and `Idempotency-Key-Status: hit`; on body mismatch it returns HTTP 409 with
    `idempotency_body_conflict`. Register the new filter in `IdempotencySetupExtensions`. Filter
    reads TTL from `IdempotencySettings` via the `PolicyName` on the existing
    `RequireIdempotencyKeyAttribute`. Add idempotency-specific metrics counter for hits, misses,
    and body conflicts
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch:
    `src/Api/emc.camus.api/Filters/IdempotencyResponseCachingFilter.cs`,
    `src/Api/emc.camus.api/Extensions/IdempotencySetupExtensions.cs`,
    `src/Api/emc.camus.api/Metrics/IdempotencyMetrics.cs`
- Adapters
  - Change summary: Implement `IIdempotencyResponseCache` in the existing
    `emc.camus.cache.inmemory` adapter using `ConcurrentDictionary` with TTL-based expiration
    (consistent with the `TokenRevocationCache` pattern); register the implementation as a singleton
    in `InMemoryCacheSetupExtensions`
  - Potential files/folders to touch:
    `src/Adapters/emc.camus.cache.inmemory/Services/IdempotencyResponseCache.cs`,
    `src/Adapters/emc.camus.cache.inmemory/InMemoryCacheSetupExtensions.cs`
- Tests
  - Change summary: Unit tests for `IdempotencyResponseCache` adapter covering store, retrieve,
    TTL expiry, and user isolation; unit tests for the new `IdempotencyResponseCachingFilter`
    covering cache miss, cache hit, body conflict, 5xx non-caching, fail-open, and user isolation
    scenarios; integration tests extending the existing `IdempotencyTestController` to verify
    end-to-end response caching through the HTTP pipeline
  - Potential files/folders to touch:
    `src/Test/emc.camus.cache.inmemory.test/Services/IdempotencyResponseCacheTests.cs`,
    `src/Test/emc.camus.api.test/Filters/IdempotencyResponseCachingFilterTests.cs`,
    `src/Test/emc.camus.api.integration.test/Common/IdempotencyInMemoryTests.cs`,
    `src/Test/emc.camus.api.integration.test/Helpers/IdempotencyTestController.cs`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: Cache entries are keyed by the composite `{authenticated-user-id}:{idempotency-key}`
  ensuring per-principal isolation; `IUserContext.GetCurrentUserId()` provides the principal; if
  the user is unauthenticated the filter must not cache (fail-open). Cache keys use the opaque
  user ID (GUID) — no PII in keys
- Performance: Use `ConcurrentDictionary` inside the `IIdempotencyResponseCache` adapter
  implementation for O(1) lookups with TTL-based expiration managed by a timestamp check on
  retrieval; hash request body with SHA256 (built-in `System.Security.Cryptography.SHA256`)
  producing a fixed-length hex string for comparison; cache hit path avoids re-reading the body
- Observability: Define an `IdempotencyMetrics` class exposing counters for
  `idempotency_cache_hit_total`, `idempotency_body_conflict_total`, and
  `idempotency_cache_error_total` using the existing OpenTelemetry meter pattern; log body
  conflict events at Warning level including trace ID via structured logging
- Reliability: Wrap all cache access in try-catch; on any cache exception, log at Error level
  and allow the request to proceed without caching (fail-open); only cache 2xx responses — 4xx
  and 5xx are not cached because idempotency's purpose is preventing double-processing of
  successful writes; failed requests are harmless to re-execute and may produce different results
  after state changes (this refines FR-06 which only excluded 5xx; the broader exclusion better
  serves the idempotency contract)
- Compliance: Cached response bodies reside solely in process memory and are evicted by TTL;
  no external persistence of cached data

### Delivery and Rollout Notes

- Rollout strategy: Full rollout — the feature activates only on endpoints already decorated
  with `[RequireIdempotencyKey]`; no feature flag needed since behavior is opt-in per endpoint
  and is additive (new response header, caching logic). Deploy after US-01 is merged
- Rollback strategy: Remove or disable the `IdempotencyResponseCachingFilter` registration in
  `IdempotencySetupExtensions`; the existing validation filter continues to function
  independently. No data migration or state cleanup required since cache is in-memory only
- Operational readiness checks: Verify `idempotency_cache_hit_total` and
  `idempotency_cache_error_total` counters appear in Prometheus/Grafana dashboards; alert on
  elevated `idempotency_body_conflict_total` rates which may indicate client integration issues;
  no runbook changes required

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect, 2026-05-15`

## Section C - Implementation Tracking

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_FirstRequestWithIdempotencyKey_ExecutesActionAndReturnsStatusMiss | Api | New |
| AC-02 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_RepeatedRequestSameKeyAndBody_ReturnsCachedResponseWithStatusHit | Api | New |
| AC-02 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_CacheHit_ReturnsOriginalStatusCode | Api | New |
| AC-03 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_SameKeyDifferentBody_Returns409WithBodyConflictErrorCode | Api | New |
| AC-04 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_RequestAfterTtlExpiry_TreatedAsNewRequest | Api | New |
| AC-04 | IdempotencyResponseCacheTests | TryGet_KeyExpiredBeyondTtl_ReturnsNull | Adapter | New |
| AC-05 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_DifferentUsersSameKey_TreatedIndependently | Api | New |
| AC-05 | IdempotencyResponseCacheTests | TryGet_DifferentKeysIsolated_ReturnsCorrectEntry | Adapter | New |
| AC-06 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_ActionThrowsException_DoesNotCacheResponse | Api | New |
| AC-06 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_ActionReturnsNonObjectResult_DoesNotCacheResponse | Api | New |
| AC-07 | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_CacheThrowsException_ProceedsWithoutCaching | Api | New |
| — | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_UnauthenticatedUser_ThrowsInvalidOperationException | Api | New |
| — | IdempotencyResponseCachingFilterTests | OnResourceExecutionAsync_NoRequireIdempotencyKeyAttribute_PassesThroughWithoutCaching | Api | New |
| — | IdempotencyResponseCacheTests | TryGet_InvalidCompositeKey_ThrowsArgumentException | Adapter | New |
| — | IdempotencyResponseCacheTests | TryGet_KeyNotInCache_ReturnsNull | Adapter | New |
| — | IdempotencyResponseCacheTests | TryGet_KeyInCacheWithinTtl_ReturnsCachedResponse | Adapter | New |
| — | IdempotencyResponseCacheTests | Store_InvalidCompositeKey_ThrowsArgumentException | Adapter | New |
| — | IdempotencyResponseCacheTests | Store_NullResponse_ThrowsArgumentNullException | Adapter | New |
| — | IdempotencyResponseCacheTests | Store_ZeroTtl_ThrowsArgumentOutOfRangeException | Adapter | New |
| — | IdempotencyResponseCacheTests | Store_NegativeTtl_ThrowsArgumentOutOfRangeException | Adapter | New |
| — | IdempotencyResponseCacheTests | Store_OverwritesExistingEntry_ReturnsNewValue | Adapter | New |
| — | IdempotencyMetricsTests | Constructor_InvalidServiceName_ThrowsArgumentException | Api | New |
| — | IdempotencyMetricsTests | RecordCacheHit_ValidCall_RecordsMetric | Api | New |
| — | IdempotencyMetricsTests | RecordCacheError_ValidCall_RecordsMetric | Api | New |
| — | IdempotencyMetricsTests | RecordBodyConflict_ValidCall_RecordsMetric | Api | New |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| Application | src/Application/emc.camus.application/Common/ErrorCodes.cs | Modified | static class ErrorCodes | IdempotencyBodyConflict (const string) |
| Application | src/Application/emc.camus.application/Common/Headers.cs | Modified | static class Headers | IdempotencyKeyStatus (const string) |
| Application | src/Application/emc.camus.application/Idempotency/IIdempotencyResponseCache.cs | New | interface IIdempotencyResponseCache | TryGet(string), Store(string, CachedResponse, TimeSpan) |
| Application | src/Application/emc.camus.application/Idempotency/CachedResponse.cs | New | sealed class CachedResponse | StatusCode, Body, BodyHash |
| Api | src/Api/emc.camus.api/Filters/IdempotencyResponseCachingFilter.cs | Modified | partial class IdempotencyResponseCachingFilter : IAsyncResourceFilter | OnResourceExecutionAsync (uses IIdempotencyResponseCache) |
| Api | src/Api/emc.camus.api/Metrics/IdempotencyMetrics.cs | New | sealed class IdempotencyMetrics : IDisposable | RecordCacheHit(), RecordBodyConflict(), RecordCacheError(), Dispose() |
| Adapter | src/Adapters/emc.camus.cache.inmemory/Services/IdempotencyResponseCache.cs | New | internal sealed class IdempotencyResponseCache : IIdempotencyResponseCache | TryGet(string), Store(string, CachedResponse, TimeSpan) |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `Unit Tester Agent, 2026-05-15`

### Developer Handoff Gate

- All tests pass (TDD green): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `Developer, 2026-05-15`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| HTTP → Validation filter → Exception middleware (missing key) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostToDecoratedEndpoint_MissingIdempotencyKeyHeader_Returns400WithMissingErrorCode | Modified |
| HTTP → Filter passthrough (no attribute) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostToUndecoratedEndpoint_NoIdempotencyKeyHeader_Returns200 | Modified |
| HTTP → Caching filter → Cache adapter → Response (miss) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostWithIdempotencyKey_FirstRequest_ReturnsOkWithMissHeader | New |
| HTTP → Caching filter → Cache adapter → Response (hit) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostWithIdempotencyKey_RepeatedRequestSameBody_ReturnsCachedResponseWithHitHeader | New |
| HTTP → Caching filter → Exception middleware (body conflict) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostWithIdempotencyKey_SameKeyDifferentBody_Returns409WithBodyConflictErrorCode | New |
| HTTP → Auth middleware → 401 (unauthenticated) | ApiInMemoryFactory | IdempotencyInMemoryTests | PostWithIdempotencyKey_UnauthenticatedUser_Returns401 | Modified |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | No failures | All integration tests pass | — |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `Integration Tester Agent, 2026-05-15`
