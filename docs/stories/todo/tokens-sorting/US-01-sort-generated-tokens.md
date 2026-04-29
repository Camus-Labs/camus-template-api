# User Story Specification

## Metadata

- Story ID: `US-01`
- Feature Slug: `tokens-sorting`
- Story Slug: `sort-generated-tokens`
- Request Date: `2026-04-28`
- Requested By: `Product Owner`

## Section A - Product Owner Definition

### Story Statement

As an `authenticated API consumer with token.create permission`, I want `to sort the list of generated tokens by a specified field and direction`, so that `I can organize token results according to my needs when reviewing them`.

### Business Value

- Enables users to quickly locate tokens by creation date, expiry, username, or revocation time
- Improves usability of the tokens listing endpoint for users managing multiple tokens

### In Scope

- Adding optional `SortBy` query parameter accepting: `tokenUsername`, `expiresOn`, `createdAt`, `revokedAt`
- Adding optional `SortDirection` query parameter accepting: `asc`, `desc`
- When no sort parameters are provided, results return in database-default order (current behavior)
- Single-field sorting only

### Out of Scope

- Multi-field sorting (sorting by more than one field simultaneously)
- Sorting on fields not listed above (e.g., `Jti`, `IsRevoked`, `IsValid`, `Permissions`)
- Changes to pagination or filtering behavior
- Backward compatibility guarantees with current default ordering

### Functional Requirements

- FR-01: The `GET /api/v2/auth/tokens` endpoint accepts an optional `SortBy` query parameter with allowed values: `tokenUsername`, `expiresOn`, `createdAt`, `revokedAt`
- FR-02: The `GET /api/v2/auth/tokens` endpoint accepts an optional `SortDirection` query parameter with allowed values: `asc`, `desc`
- FR-03: When `SortBy` is provided without `SortDirection`, the API returns a 400 Bad Request validation error
- FR-04: When `SortDirection` is provided without `SortBy`, the API returns a 400 Bad Request validation error
- FR-05: When neither `SortBy` nor `SortDirection` is provided, results are returned in database-default order
- FR-06: When an invalid value is provided for `SortBy` or `SortDirection`, the API returns a 400 Bad Request with a descriptive error message
- FR-07: Sorting is applied before pagination (sort the full result set, then paginate)

### Non-Functional Requirements

- Security: No additional security constraints beyond existing JWT + `token.create` permission requirement
- Performance: Sorting must not degrade endpoint response time beyond current latency for typical result sets
- Observability: Sort parameters (`sort_by`, `sort_direction`) are included in activity/trace tags for the endpoint
- Reliability: No additional reliability requirements beyond existing endpoint behavior
- Compliance: No additional compliance requirements

### Acceptance Criteria

- AC-01: A request with `?sortBy=createdAt&sortDirection=desc` returns tokens ordered by creation date descending
- AC-02: A request with `?sortBy=expiresOn&sortDirection=asc` returns tokens ordered by expiration date ascending
- AC-03: A request with `?sortBy=tokenUsername&sortDirection=asc` returns tokens ordered alphabetically by token username
- AC-04: A request with `?sortBy=revokedAt&sortDirection=desc` returns tokens ordered by revocation date descending (null values sorted last)
- AC-05: A request without sort parameters returns tokens in database-default order
- AC-06: A request with `?sortBy=invalidField&sortDirection=asc` returns 400 Bad Request with a validation error
- AC-07: A request with `?sortBy=createdAt` (missing direction) returns 400 Bad Request with a validation error
- AC-08: A request with `?sortDirection=asc` (missing sortBy) returns 400 Bad Request with a validation error
- AC-09: Sorting is applied before pagination — page 2 of sorted results contains the correct subset

### Constraints and Dependencies

- Business constraints:
  - None
- Dependencies:
  - None

### Risks and Open Questions

- Risks:
  - Sorting by `revokedAt` involves nullable column — null-handling behavior (nulls last) must be consistent across database engines
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
- Product Owner sign-off: `Product Owner, 2026-04-28`

## Section B - Architect Definition

Do not include code snippets.

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
- Tester sign-off: `[Name, Date]`

### Developer Handoff Gate

- All tests pass (TDD green): `[Yes | No]`
- Build succeeds with zero warnings: `[Yes | No]`
- Ready for code review: `[Yes | No]`
- Developer sign-off: `[Name, Date]`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| [cross-layer boundary] | [factory class name] | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [New, Modified, Existing] |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| [n] | [test method] | [failure description] | [analysis] | [production file path] |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `[Yes | No]`
- All integration tests pass: `[Yes | No]`
- No unresolved production code findings: `[Yes | No]`
- Ready for review: `[Yes | No]`
- Integration Tester sign-off: `[Name, Date]`
