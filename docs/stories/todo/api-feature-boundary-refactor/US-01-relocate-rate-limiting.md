# User Story Specification

## Metadata

- Story ID: `US-01`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `relocate-rate-limiting`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the rate-limiting feature relocated from Adapters into the API layer with its partition store consumed
through the existing cache port`, so that `the architectural boundary between API pipeline features and
true infrastructure adapters is clarified, and a future distributed cache adapter automatically enables
multi-instance rate limiting`.

### Business Value

- Enforces hexagonal architecture discipline: only true port/adapter pairs live under Adapters
- Enables future distributed rate limiting (Redis, etc.) by routing partition storage through the cache port without
  touching the rate-limiting feature code
- Reduces cognitive overhead when onboarding — developers know Adapters == swappable external tech

### In Scope

- Move all source files from `src/Adapters/emc.camus.ratelimiting.inmemory/` into `src/Api/emc.camus.api/` following the
  Configurations/ + Middleware/ + Extensions/\*SetupExtensions.cs + Metrics/ pattern
- Refactor the in-memory partition store to consume `emc.camus.cache.inmemory` through the Application-layer cache port
  (mirroring IdempotencyResponseCache pattern)
- Update the `emc.camus.api.csproj` to remove the project reference to `emc.camus.ratelimiting.inmemory`
- Remove the `emc.camus.ratelimiting.inmemory` project from the solution
- Delete the `src/Adapters/emc.camus.ratelimiting.inmemory/` directory
- Rename settings class from `InMemoryRateLimitingSettings` to `RateLimitingSettings` and rename the `appsettings`
  configuration section from `InMemoryRateLimitingSettings` to `RateLimitingSettings` (the "InMemory" qualifier was an
  adapter implementation detail; the feature now routes partition storage through the cache port)
- Update all `appsettings*.json` files to use the new `RateLimitingSettings` section name
- Update `Program.cs` composition root to wire rate limiting from the API project directly
- Maintain identical HTTP behavior: same response headers, same 429 responses, same policies
- Maintain identical configuration schema within the section (Policies, ExemptPaths keys unchanged)

### Out of Scope

- Implementing a distributed cache adapter (Redis, etc.)
- Changing rate-limiting algorithms (sliding window stays)
- Modifying rate-limiting policies, exempt paths, or the internal configuration schema (Policies, ExemptPaths keys stay
  the same)
- Changing the Application-layer `RateLimiting/` contracts (port interfaces, attributes)
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: All rate-limiting source files are located under `src/Api/emc.camus.api/` in appropriate subdirectories
  (Configurations/, Middleware/, Extensions/, Metrics/)
- FR-02: The rate-limiting partition store reads/writes through an Application-layer cache port interface implemented by
  `emc.camus.cache.inmemory`
- FR-03: The `emc.camus.ratelimiting.inmemory` project is removed from the solution and its directory deleted
- FR-04: The API project no longer holds a project reference to `emc.camus.ratelimiting.inmemory`
- FR-05: Rate-limiting DI registration occurs via an extension method in `src/Api/emc.camus.api/Extensions/`
- FR-06: The settings class is renamed from `InMemoryRateLimitingSettings` to `RateLimitingSettings`
- FR-07: The `appsettings*.json` configuration section is renamed from `InMemoryRateLimitingSettings` to
  `RateLimitingSettings` with identical internal schema (Policies, ExemptPaths)

### Non-Functional Requirements

- Security: No change — rate limiting continues protecting endpoints against abuse
- Performance: No performance considerations; cache port indirection is acceptable
- Observability: Existing rate-limiting metrics remain functional with same metric names
- Reliability: Single coordinated release; no intermediate broken state
- Compliance: N/A

### Acceptance Criteria

- AC-01: Solution builds successfully without `emc.camus.ratelimiting.inmemory` project reference
- AC-02: All rate-limiting source files exist under `src/Api/emc.camus.api/` in the correct subdirectories
- AC-03: Rate-limiting partition data is stored/retrieved via the Application-layer cache port (verified by DI
  registration and code inspection)
- AC-04: HTTP responses for rate-limited requests return identical headers (`RateLimit-Limit`, `RateLimit-Reset`,
  `RateLimit-Policy`, `RateLimit-Window`, `Retry-After`) and status code 429
- AC-05: Existing rate-limiting metrics continue emitting with the same metric names and dimensions
- AC-06: The `appsettings*.json` files use `RateLimitingSettings` as the section name with the same internal keys
  (Policies, ExemptPaths)
- AC-07: The settings class bound via DI is named `RateLimitingSettings` (no `InMemory` prefix)

### Constraints and Dependencies

- Business constraints:
  - Must be delivered before US-02, US-03, US-04 (sequential ordering)
  - Single coordinated release with US-05
- Dependencies:
  - `emc.camus.cache.inmemory` adapter must expose (or already exposes) a cache port suitable for partition storage
  - Application-layer `RateLimiting/` contracts remain unchanged

### Risks and Open Questions

- Risks:
  - Cache port interface may need a new method for sliding-window partition semantics — mitigation: review existing
    `IRateLimitCounterStore` or equivalent and map to cache port; owner: Tech Lead
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
  - Change summary: No changes — rate limiting has no domain-layer behavior
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No new port interface needed. Sliding-window rate limiting requires atomic check-and-increment
    semantics that cannot be meaningfully abstracted behind a simple cache port — a distributed implementation (Redis)
    would swap the entire `PartitionedRateLimiter` at the composition root, not just a storage backend. Existing
    `RateLimitAttribute` and `RateLimitPolicies` remain unchanged.
  - Potential files/folders to touch: `None`
- Database Schema
  - Change summary: No changes — rate-limit counters remain in-memory only
  - Potential files/folders to touch: `None`
- API
  - Change summary: Receive all rate-limiting source files relocated from the adapter as-is, including the internal
    `ConcurrentDictionary`-based partition storage. Files organized into Configurations/ (settings class), Middleware/
    (response headers middleware), Extensions/ (DI setup extension method), Metrics/ (rate-limit metrics class), and
    Infrastructure/ (IP resolver helper). The setup extension wires ASP.NET Core `PartitionedRateLimiter` directly — no
    port indirection. No HTTP surface changes — identical headers, status codes, and behavior. A future distributed
    rate-limiting story would introduce a new adapter providing a `PartitionedRateLimiter` implementation swapped at the
    composition root.
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch: `src/Api/emc.camus.api/Configurations/`, `src/Api/emc.camus.api/Middleware/`,
    `src/Api/emc.camus.api/Extensions/`, `src/Api/emc.camus.api/Metrics/`, `src/Api/emc.camus.api/Infrastructure/`,
    `src/Api/emc.camus.api/emc.camus.api.csproj`, `src/Api/emc.camus.api/Program.cs`
- Adapters
  - Change summary: Remove the entire `emc.camus.ratelimiting.inmemory` project from the solution and delete its
    directory. No changes to `emc.camus.cache.inmemory` — rate-limit partition storage stays internal to the API layer.
  - Potential files/folders to touch: `src/Adapters/emc.camus.ratelimiting.inmemory/` (deletion)
- Tests
  - Change summary: Out of scope for this story (covered in US-05). Existing unit tests for the rate-limiting adapter
    will be relocated in the companion story.
  - Potential files/folders to touch: `None (deferred to US-05)`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No change in behavior. Rate limiting continues to protect all non-exempt endpoints using IP-based sliding
  window policies. The relocation is purely structural; partition logic, rejection responses, and header emission remain
  identical.
- Performance: No new indirection — the `ConcurrentDictionary`-based partition storage is relocated as-is within the API
  layer. Zero additional virtual dispatch or allocation overhead compared to the current adapter.
- Observability: The `RateLimitMetrics` class is relocated as-is with the same meter name, instrument names, and tag
  dimensions. No metric rename or schema change. Existing dashboards and alerts remain valid.
- Reliability: Single coordinated release — the adapter removal and API-layer addition happen in the same commit. No
  intermediate broken state. The solution must build and all existing tests must pass before merge.

### Delivery and Rollout Notes

- Rollout strategy: Full rollout in a single coordinated release alongside US-05 (test relocation). No feature flag
  needed — this is a structural refactor with identical runtime behavior.
- Rollback strategy: Revert the merge commit. The previous adapter project and its reference are restored by git
  history. No data migration or state to reconcile.
- Operational readiness checks: Verify rate-limiting metrics (`rate_limit_exceeded` counter, partition gauges) continue
  emitting post-deploy. Confirm 429 responses and response headers match baseline via existing integration or smoke
  tests.

### Architect Handoff Readiness

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Rollout and rollback strategies defined: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect (Copilot), 2026-05-27`

## Section C - Implementation Tracking

### Test Traceability

| AC    | Test Class                      | Test Method                                                            | Layer | Change |
| ----- | ------------------------------- | ---------------------------------------------------------------------- | ----- | ------ |
| AC-01 | RateLimitingSettingsTests       | ConfigurationSectionName_ReturnsRateLimitingSettings                   | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_ValidContext_SetsRateLimitLimitHeader                      | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_ValidContext_SetsRateLimitResetHeader                      | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_ValidContext_SetsRateLimitPolicyHeader                     | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_ValidContext_SetsRateLimitWindowHeader                     | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_ValidContext_CallsNextDelegate                             | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_MissingPolicyContextItem_SetsUnknownPolicyHeader           | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_MissingLimitContextItem_SetsUnknownLimitHeader             | Api   | New    |
| AC-04 | RateLimitHeadersMiddlewareTests | InvokeAsync_InvalidWindowValue_DoesNotSetResetHeader                   | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_ValidForwardedFor_ReturnsFirstIp                    | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_InvalidForwardedFor_FallsBackToRealIp               | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_ValidRealIp_ReturnsRealIp                           | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_RemoteIpOnly_ReturnsRemoteIp                        | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_NoIpAvailable_ThrowsInvalidOperationException       | Api   | New    |
| AC-04 | ClientIpResolverTests           | GetClientIpAddress_NullContext_ThrowsArgumentNullException             | Api   | New    |
| AC-04 | ClientIpResolverTests           | Constructor_NullLogger_ThrowsArgumentNullException                     | Api   | New    |
| AC-05 | RateLimitMetricsTests           | RecordRejection_ValidParameters_RecordsMetricWithCorrectName           | Api   | New    |
| AC-05 | RateLimitMetricsTests           | RecordRejection_InvalidPolicyName_ThrowsArgumentException              | Api   | New    |
| AC-05 | RateLimitMetricsTests           | RecordRejection_InvalidMethod_ThrowsArgumentException                  | Api   | New    |
| AC-05 | RateLimitMetricsTests           | Constructor_InvalidServiceName_ThrowsArgumentException                 | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_DefaultSettings_DoesNotThrow                                  | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_ValidSegmentsPerWindow_DoesNotThrow                           | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_SegmentsPerWindowOutOfRange_ThrowsInvalidOperationException   | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_NullPolicies_ThrowsInvalidOperationException                  | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_EmptyPolicies_ThrowsInvalidOperationException                 | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_MissingDefaultPolicy_ThrowsInvalidOperationException          | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_NullPolicyValue_ThrowsInvalidOperationException               | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_PolicyWithInvalidPermitLimit_ThrowsInvalidOperationException  | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_EmptyExemptPaths_DoesNotThrow                                 | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_NullExemptPaths_ThrowsInvalidOperationException               | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_ExemptPathWithInvalidEntry_ThrowsInvalidOperationException    | Api   | New    |
| AC-06 | RateLimitingSettingsTests       | Validate_ExemptPathWithoutLeadingSlash_ThrowsInvalidOperationException | Api   | New    |
| AC-06 | RateLimitPolicySettingsTests    | Validate_ValidSettings_DoesNotThrow                                    | Api   | New    |
| AC-06 | RateLimitPolicySettingsTests    | Validate_EmptyOrWhitespacePolicyName_ThrowsInvalidOperationException   | Api   | New    |
| AC-06 | RateLimitPolicySettingsTests    | Validate_PermitLimitOutOfRange_ThrowsInvalidOperationException         | Api   | New    |
| AC-06 | RateLimitPolicySettingsTests    | Validate_WindowSecondsOutOfRange_ThrowsInvalidOperationException       | Api   | New    |
| AC-07 | RateLimitingSettingsTests       | ConfigurationSectionName_ReturnsRateLimitingSettings                   | Api   | New    |

### Skeleton Inventory

| Layer | Stub File                                                       | Change   | Types                                    | Members                                                                         |
| ----- | --------------------------------------------------------------- | -------- | ---------------------------------------- | ------------------------------------------------------------------------------- |
| Api   | src/Api/emc.camus.api/Configurations/RateLimitingSettings.cs    | New      | class RateLimitingSettings               | SegmentsPerWindow, Policies, ExemptPaths, Validate()                            |
| Api   | src/Api/emc.camus.api/Configurations/RateLimitPolicySettings.cs | New      | class RateLimitPolicySettings            | PolicyName, PermitLimit, WindowSeconds, Validate()                              |
| Api   | src/Api/emc.camus.api/Middleware/RateLimitHeadersMiddleware.cs  | New      | class RateLimitHeadersMiddleware         | InvokeAsync(HttpContext)                                                        |
| Api   | src/Api/emc.camus.api/Extensions/RateLimitingSetupExtensions.cs | New      | static class RateLimitingSetupExtensions | AddRateLimiting(WebApplicationBuilder, string), UseRateLimiting(WebApplication) |
| Api   | src/Api/emc.camus.api/Metrics/RateLimitMetrics.cs               | New      | class RateLimitMetrics                   | RecordRejection(string, string), Dispose()                                      |
| Api   | src/Api/emc.camus.api/Infrastructure/ClientIpResolver.cs        | New      | class ClientIpResolver                   | GetClientIpAddress(HttpContext)                                                 |
| Api   | src/Api/emc.camus.api/Infrastructure/RateLimitContextKeys.cs    | New      | static class RateLimitContextKeys        | Policy, Limit, Window                                                           |
| Api   | src/Api/emc.camus.api/emc.camus.api.csproj                      | Modified | —                                        | Added InternalsVisibleTo for emc.camus.api.test                                 |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `Unit Tester (Copilot), 2026-05-27`

### Regression Fixes Log

| #   | Test File                                                                  | Test Method   | Change Made                                                                         | Reason                                        |
| --- | -------------------------------------------------------------------------- | ------------- | ----------------------------------------------------------------------------------- | --------------------------------------------- |
| 1   | src/Test/emc.camus.api.integration.test/Fixtures/ApiFactoryBase.cs         | N/A (fixture) | Renamed `InMemoryRateLimitingSettings` → `RateLimitingSettings` in UseSetting calls | Configuration section renamed per FR-06/FR-07 |
| 2   | src/Test/emc.camus.api.integration.test/Fixtures/ApiRateLimitingFactory.cs | N/A (fixture) | Renamed `InMemoryRateLimitingSettings` → `RateLimitingSettings` in UseSetting calls | Configuration section renamed per FR-06/FR-07 |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-05-28`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary                                                                     | Factory                | Test Class                     | Test Method                                                                                   | Change   |
| ---------------------------------------------------------------------------- | ---------------------- | ------------------------------ | --------------------------------------------------------------------------------------------- | -------- |
| HTTP pipeline → Rate limiting middleware → 429 rejection with headers        | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | SameIp_ExceedsPermitLimit_Returns429WithErrorCodeAndHeaders                                   | Existing |
| HTTP pipeline → Rate limiting → IP-independent partition buckets             | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | DifferentIps_SameEndpoint_HaveIndependentRateLimitBuckets                                     | Existing |
| HTTP pipeline → Rate limiting → Exhausted IP remains throttled               | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | ExhaustedIp_SameEndpoint_RemainsThrottled                                                     | Existing |
| HTTP pipeline → Rate limiting → Both IPs exhaust independently               | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | DifferentIps_BothExhaustLimitsIndependently_BothGetThrottled                                  | Existing |
| HTTP pipeline → Rate limiting → Relaxed policy throttling                    | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | RelaxedPolicy_ThrottlesAtRelaxedLimit_NotDefaultOrStrictLimit                                 | Existing |
| HTTP pipeline → Rate limiting → Default policy throttling                    | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | DefaultPolicy_ThrottlesAtDefaultLimit_NotRelaxedOrStrictLimit                                 | Existing |
| HTTP pipeline → Rate limiting → Strict policy throttling                     | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | StrictPolicy_ThrottlesAtStrictLimit_LowerThanOtherPolicies                                    | Existing |
| HTTP pipeline → Rate limiting → Sliding window reset                         | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | SameIp_AfterWindowResets_PermitsAreReplenished                                                | Existing |
| HTTP pipeline → Rate limiting → Response headers on anonymous request        | ApiInMemoryFactory     | MiddlewareHeadersInMemoryTests | AnonymousRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAnonymousUsername            | Existing |
| HTTP pipeline → Rate limiting → Response headers on JWT request              | ApiInMemoryFactory     | MiddlewareHeadersInMemoryTests | AuthenticatedJwtRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAuthenticatedUsername | Existing |
| HTTP pipeline → Rate limiting → Response headers on API key request          | ApiInMemoryFactory     | MiddlewareHeadersInMemoryTests | AuthenticatedApiKeyRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndApiKeyUsername     | Existing |
| Configuration → RateLimitingSettings section binding → Rate limiter behavior | ApiRateLimitingFactory | RateLimitingIpPartitionTests   | RelaxedPolicy_ThrottlesAtRelaxedLimit_NotDefaultOrStrictLimit                                 | Existing |

### Integration Test Findings

| #   | Test | Failure     | Root Cause Analysis | Affected File |
| --- | ---- | ----------- | ------------------- | ------------- |
| —   | —    | No failures | —                   | —             |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `Integration Tester (Copilot), 2026-05-28`

## Section E - Technical Writer

### Status

`DOCUMENTED`

### Version Update

- Previous version: `1.0.0`
- New version: `1.0.1`
- Bump type: `PATCH`
- Reason: Internal structural refactor with no user-facing API changes

### CHANGELOG Entry

```markdown
## [1.0.1] - 2026-05-28

### Changed

- Relocate rate-limiting feature from Adapters layer into the API layer to clarify architectural boundaries
- Rename `InMemoryRateLimitingSettings` configuration section to `RateLimitingSettings`

### Removed

- Remove `emc.camus.ratelimiting.inmemory` adapter project from the solution
```

### Documentation Updates

- Swagger annotations updated: 0 endpoint(s) — no new or changed HTTP endpoints
- Postman requests updated: 0 request(s) — no endpoint changes
- Files modified: `src/Directory.Build.props`, `CHANGELOG.md`

### Technical Writer Handoff Gate

- Version in Directory.Build.props matches confirmed decision: `Yes`
- CHANGELOG entry matches new version and date: `Yes`
- Swagger examples reflect new/changed endpoints: `N/A`
- Postman collection reflects new/changed requests: `N/A`
- Markdown linting passes with zero errors: `Yes`
- Build succeeds with zero errors and warnings: `Yes`
- Technical Writer sign-off: `Technical Writer (Copilot), 2026-05-28`

Unresolved Blockers: None
