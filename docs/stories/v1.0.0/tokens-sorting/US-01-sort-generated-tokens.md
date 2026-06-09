# User Story Specification

## Metadata

- Story ID: `US-01`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As an `authenticated API consumer with token.create permission`, I want `to sort the list of generated tokens by a
specified field and direction`, so that `I can organize token results according to my needs when reviewing them`.

### Functional Requirements

- FR-01: The `GET /api/v2/auth/tokens` endpoint accepts an optional `SortBy` query parameter with allowed values:
`tokenUsername`, `expiresOn`, `createdAt`, `revokedAt`
- FR-02: The `GET /api/v2/auth/tokens` endpoint accepts an optional `SortDirection` query parameter with allowed values:
`asc`, `desc`
- FR-03: When `SortBy` is provided without `SortDirection`, the API returns a 400 Bad Request validation error
- FR-04: When `SortDirection` is provided without `SortBy`, the API returns a 400 Bad Request validation error
- FR-05: When neither `SortBy` nor `SortDirection` is provided, results are returned in `created_at DESC` order
- FR-06: When an invalid value is provided for `SortBy` or `SortDirection`, the API returns a 400 Bad Request with a
descriptive error message
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
- AC-03: A request with `?sortBy=tokenUsername&sortDirection=asc` returns tokens ordered
alphabetically by token username
- AC-04: A request with `?sortBy=revokedAt&sortDirection=desc` returns tokens ordered by revocation date descending
(null values sorted last)
- AC-05: A request without sort parameters returns tokens in database-default order
- AC-06: A request with `?sortBy=invalidField&sortDirection=asc` returns 400 Bad Request with a validation error
- AC-07: A request with `?sortBy=createdAt` (missing direction) returns 400 Bad Request with a validation error
- AC-08: A request with `?sortDirection=asc` (missing sortBy) returns 400 Bad Request with a validation error
- AC-09: Sorting is applied before pagination — page 2 of sorted results contains the correct subset

### Notes

- Sorting by `revokedAt` involves nullable column — null-handling behavior (nulls last) must be consistent
  across database engines

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `Product Owner, 2026-04-28`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes. Sorting is a query concern; domain entities (`GeneratedToken`) and business rules are
    unaffected. The sortable fields (`TokenUsername`, `ExpiresOn`, `CreatedAt`, `RevokedAt`) already
    exist on the entity.
  - Potential files/folders to touch: `None`
- Application
  - Change summary: Introduce sorting contract types to the `Auth` namespace. Add a sort parameters type (sort field
    enum and sort direction enum) to express the allowed sort fields and directions. Extend `GeneratedTokenFilter` (or
    introduce a sibling record) to carry the optional sort field and sort direction. Update the `IAuthService` and
    `AuthService.GetGeneratedTokensAsync` signatures to accept the sort parameters. Update the
    `IGeneratedTokenRepository.GetPagedByCreatorUserIdAsync` signature to accept the sort parameters so the persistence
    adapter can apply ORDER BY at the database level (FR-07: sorting before pagination).
  - Potential files/folders to touch: `src/Application/emc.camus.application/Auth/AuthFilters.cs`,
    `src/Application/emc.camus.application/Auth/IGeneratedTokenRepository.cs`,
    `src/Application/emc.camus.application/Auth/IAuthService.cs`,
    `src/Application/emc.camus.application/Auth/AuthService.cs`
- Database Schema
  - Change summary: No new migrations required. The sortable columns (`token_username`, `expires_on`, `created_at`,
    `revoked_at`) already exist on `camus.generated_tokens`. Existing indexes on `creator_user_id` and `expires_on`
    partially support sorting. A composite index on `(creator_user_id, created_at)` may be added in a future performance
    story if profiling indicates a need, but is not required for correctness now.
  - Potential files/folders to touch: `None`
- API
  - Change summary: Add optional `SortBy` and `SortDirection` query parameters to `GetGeneratedTokensQuery`. Add
    cross-property validation so that both must be present or both absent (FR-03, FR-04). Add value validation for
    allowed enum values (FR-06). Extend `AuthMappingExtensions.ToFilter` to map the new query parameters to the
    application-layer sort parameters. Include `sort_by` and `sort_direction` in the activity/trace tags set on the
    `GetGeneratedTokens` action.
  - Backward compatibility: `Backward compatible` — both parameters are optional and when omitted the endpoint behaves
    identically to today (database-default order)
  - Potential files/folders to touch: `src/Api/emc.camus.api/Models/Requests/V2/GetGeneratedTokensQuery.cs`,
    `src/Api/emc.camus.api/Mapping/V2/AuthMappingExtensions.cs`,
    `src/Api/emc.camus.api/Controllers/AuthController.cs`
- Adapters
  - Change summary: Update `GeneratedTokenRepository.GetPagedByCreatorUserIdAsync` in the PostgreSQL persistence adapter
    to accept the sort parameters and construct the ORDER BY clause. The sort field must be mapped to a safe,
    allow-listed column name (not user input directly) to prevent SQL injection. The
    `IGeneratedTokenDataAccess` interface
    and its Dapper implementation must be updated to accept the sort column and direction and apply them in the SQL
    query. For `revokedAt` sorting, use `NULLS LAST` to satisfy AC-04.
  - Potential files/folders to touch:
    `src/Adapters/emc.camus.persistence.postgresql/Repositories/GeneratedTokenRepository.cs`,
    `src/Adapters/emc.camus.persistence.postgresql/DataAccess/IGeneratedTokenDataAccess.cs`,
    `src/Adapters/emc.camus.persistence.postgresql/DataAccess/GeneratedTokenDataAccess.cs`
- Tests
  - Change summary: Add unit tests for the new application-layer sort parameter types and validation. Add unit tests for
    `GetGeneratedTokensQuery` cross-property validation (both present, both absent, one missing). Add unit tests for
    `AuthMappingExtensions` sort parameter mapping. Add unit tests for
    `AuthService.GetGeneratedTokensAsync` pass-through
    of sort parameters. Add unit tests for `GeneratedTokenRepository` to verify correct ORDER BY clause construction
    and null handling. Update existing integration tests to cover sorted pagination scenarios (AC-01 through AC-09).
  - Potential files/folders to touch: `src/Test/emc.camus.api.test/`,
    `src/Test/emc.camus.application.test/`,
    `src/Test/emc.camus.persistence.postgresql.test/`,
    `src/Test/emc.camus.api.integration.test/`

### Cross-Cutting Concern Decisions

- Security: Sort field mapping in the persistence adapter must use an allow-list of column names (enum-to-column
  dictionary) rather than interpolating user-provided strings into SQL. This prevents SQL injection even though the API
  layer validates the enum values. No additional AuthN/AuthZ changes needed — existing JWT + `token.create` permission
  requirement remains unchanged.
- Performance: Sorting is performed at the database level (ORDER BY in SQL) before pagination (LIMIT/OFFSET), so only
  the requested page is materialized in memory. Existing indexes on `creator_user_id` and `expires_on` provide partial
  coverage. No new indexes are required at this time; if query profiling reveals degradation for specific sort fields, a
  composite index migration can be added as a follow-up.
- Observability: Add `sort_by` and `sort_direction` as activity/trace tags in the `GetGeneratedTokens` controller action
  alongside the existing pagination and filter tags. No new metrics or alert rules required.
- Reliability: No additional reliability measures needed. When sort parameters are absent, the endpoint falls back to
  database-default ordering (current behavior), ensuring no behavioral regression.
- Compliance: No additional compliance requirements.
- Rollout: Full rollout. Both query parameters are optional and additive; the endpoint is fully backward
  compatible. No feature flag needed.
- Rollback: Revert the deployment to the prior version. No database migration is involved, so rollback is a
  code-only redeployment with zero data impact.
- Operational readiness: Verify `sort_by` and `sort_direction` tags appear in traces after deployment. Confirm
  endpoint latency remains within existing baseline via existing Grafana dashboards.

### Architect Handoff Gate

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `Architect, 2026-04-28`

## Section C - Implementation Tracking

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| AC-01..04 | AuthMappingExtensionsTests | ToSortParams_ValidFieldAndDirection_ReturnsMappedSortParams [Theory] | Api | New |
| AC-01..04 | GeneratedTokenRepositoryTests | GetPagedByCreatorUserIdAsync_WithSort_PassesMappedColumnAndDirectionToDataAccess [Theory] | Adapter | New |
| AC-05 | AuthMappingExtensionsTests | ToSortParams_BothNull_ReturnsNull | Api | New |
| AC-05 | GeneratedTokenRepositoryTests | GetPagedByCreatorUserIdAsync_WithoutSort_PassesNullSortToDataAccess | Adapter | New |
| AC-06 | AuthMappingExtensionsTests | ToSortParams_InvalidSortBy_ThrowsArgumentException | Api | New |
| AC-07..08 | AuthMappingExtensionsTests | ToSortParams_OnlyOneProvided_ThrowsArgumentException [Theory] | Api | New |
| AC-09 | GeneratedTokenRepositoryTests | GetPagedByCreatorUserIdAsync_WithSort_PassesMappedColumnAndDirectionToDataAccess [Theory] | Adapter | New |
| N/A | SortDirectionExtensionsTests | ToSql_Asc_ReturnsASC | Application | New |
| N/A | SortDirectionExtensionsTests | ToSql_Desc_ReturnsDESC | Application | New |
| N/A | GeneratedTokenSortParamsTests | Constructor_ValidFieldAndDirection_SetsProperties [Theory] | Application | New |

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| Application | `src/Application/emc.camus.application/Common/SortDirection.cs` | New | enum `SortDirection`, static class `SortDirectionExtensions` | `Asc`, `Desc`, `ToSql()` |
| Application | `src/Application/emc.camus.application/Auth/AuthSorting.cs` | New | enum `GeneratedTokenSortField`, record `GeneratedTokenSortParams` | `TokenUsername`, `ExpiresOn`, `CreatedAt`, `RevokedAt`, `Field`, `Direction` |
| Application | `src/Application/emc.camus.application/Auth/IAuthService.cs` | Modified | interface `IAuthService` | `GetGeneratedTokensAsync` — added `GeneratedTokenSortParams sort` |
| Application | `src/Application/emc.camus.application/Auth/IGeneratedTokenRepository.cs` | Modified | interface `IGeneratedTokenRepository` | `GetPagedByCreatorUserIdAsync` — added `GeneratedTokenSortParams sort` |
| Application | `src/Application/emc.camus.application/Auth/AuthService.cs` | Modified | class `AuthService` | `GetGeneratedTokensAsync` — passes `sort` to repository |
| Api | `src/Api/emc.camus.api/Models/Requests/V2/GetGeneratedTokensQuery.cs` | Modified | class `GetGeneratedTokensQuery` | Added `SortBy`, `SortDirection` string? properties |
| Api | `src/Api/emc.camus.api/Mapping/V2/AuthMappingExtensions.cs` | Modified | static class `AuthMappingExtensions` | Added `ToSortParams()` — parses `SortBy`/`SortDirection` into `GeneratedTokenSortParams` |
| Api | `src/Api/emc.camus.api/Controllers/AuthController.cs` | Modified | class `AuthController` | `GetGeneratedTokens` — calls `ToSortParams()`, passes sort, adds trace tags |
| Adapter | `src/Adapters/emc.camus.persistence.postgresql/Repositories/GeneratedTokenRepository.cs` | Modified | class `GeneratedTokenRepository` | Added `SortFieldColumnMap`, maps sort to column/direction via `ToSql()` |
| Adapter | `src/Adapters/emc.camus.persistence.postgresql/DataAccess/IGeneratedTokenDataAccess.cs` | Modified | interface `IGeneratedTokenDataAccess` | `GetPageByCreatorUserIdAsync` — added `sortColumn`, `sortDirection` optional params |
| Adapter | `src/Adapters/emc.camus.persistence.postgresql/DataAccess/GeneratedTokenDataAccess.cs` | Modified | class `GeneratedTokenDataAccess` | `GetPageByCreatorUserIdAsync` — applies ORDER BY with mapped column + direction and NULLS LAST |

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes` — all tests initially failed before implementation;
Application/Adapter tests pass because their production code (enums, dictionary map, `ToSql()`) is trivially
complete
- Ready for developer implementation: `Yes`
- Tester sign-off: `Unit Tester, 2026-04-28`

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
| — | — | — | None | No regressions recorded |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `N/A`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `Developer, 2026-04-28`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| HTTP → Controller → Service → Repo → DB (sorted query) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GetTokens_SortByCreatedAtDesc_ReturnsTokensInCorrectOrder | New |
| HTTP → Controller validation (invalid sort params → 400) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GetTokens_SortByWithoutDirection_ReturnsBadRequest | New |
| HTTP → Controller → Service → Repo → DB (sort + pagination) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GetTokens_SortByCreatedAtWithPagination_ReturnsCorrectPageSubset | New |
| HTTP → Controller → Service → Repo → DB (no sort = default order) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GetTokens_AfterGenerating_ReturnsPagedTokensFromDatabase | Existing |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | No failures | All 57 integration tests passed | — |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `Integration Tester, 2026-04-29`
