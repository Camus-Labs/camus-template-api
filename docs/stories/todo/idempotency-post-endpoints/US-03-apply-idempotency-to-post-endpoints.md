# User Story Specification

## Metadata

- Story ID: `US-03`
- Feature Slug: `idempotency-post-endpoints`
- Story Slug: `apply-idempotency-to-post-endpoints`
- Request Date: `2026-05-02`
- Requested By: `Internal Service Team`

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

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-02 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-03 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| [Domain, Application, Api, Adapter] | [src/.../FileName.cs] | [New, Modified] | [class, interface, record] | [method signatures, properties] |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `[Yes | No]`
- Skeleton inventory complete and user-approved: `[Yes | No]`
- Tests compile and fail for the right reason (TDD red): `[Yes | No]`
- Ready for implementation: `[Yes | No]`
