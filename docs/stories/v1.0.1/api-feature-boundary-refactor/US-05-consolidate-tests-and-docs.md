# User Story Specification

## Metadata

- Story ID: `US-05`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the tests from relocated adapter projects consolidated into emc.camus.api.test and
emc.camus.api.integration.test, and all affected documentation updated`, so that
`the test structure mirrors the new project layout and documentation accurately reflects the
architectural boundary between API features and adapters`.

### Functional Requirements

- FR-01: The 4 adapter directories (`ratelimiting.inmemory`, `security.jwt`, `security.apikey`,
  `documentation.swagger`) are deleted from `src/Adapters/`
- FR-02: The 4 adapter projects are removed from `CamusApp.sln` and all project references to them
  are removed from `emc.camus.api.csproj`
- FR-03: `Program.cs` calls the new API-layer setup extensions; no references to deleted adapter
  namespaces remain
- FR-04: `emc.camus.api.csproj` directly references any NuGet packages previously obtained
  transitively through the deleted adapters
- FR-05: All unit tests from the 4 removed adapter test projects exist in
  `src/Test/emc.camus.api.test/` with appropriate subdirectory organization
- FR-06: All integration tests from the 4 removed adapter test projects exist in
  `src/Test/emc.camus.api.integration.test/`
- FR-07: The 4 orphaned test projects are removed from the solution and their directories deleted
- FR-08: Solution filters (`UnitTests.slnf`, `IntegrationTests.slnf`) reference only existing projects
- FR-09: `README.md` project structure section accurately reflects the new layout
- FR-10: `docs/architecture.md` accurately describes the boundary between API features and true adapters
- FR-11: Adapter READMEs for the 4 relocated features are removed or archived

### Non-Functional Requirements

- Security: N/A
- Performance: N/A
- Observability: N/A
- Reliability: All existing tests pass — 100% coverage preserved with adjustments where needed
- Compliance: N/A

### Acceptance Criteria

- AC-01: `dotnet build src/CamusApp.sln` succeeds — no references to deleted adapter projects or namespaces
- AC-02: `dotnet test src/CamusApp.sln` passes with zero failures
- AC-03: The directories `src/Adapters/emc.camus.ratelimiting.inmemory/`,
  `src/Adapters/emc.camus.security.jwt/`, `src/Adapters/emc.camus.security.apikey/`,
  `src/Adapters/emc.camus.documentation.swagger/` do not exist
- AC-04: No references to `emc.camus.ratelimiting.inmemory`, `emc.camus.security.jwt`,
  `emc.camus.security.apikey`, or `emc.camus.documentation.swagger` exist in `.sln`, `.slnf`,
  or `.csproj` files
- AC-05: `Program.cs` calls only API-layer setup extensions for rate limiting, JWT, API Key,
  and Swagger
- AC-06: `README.md` project structure matches the actual directory layout
- AC-07: `docs/architecture.md` lists only true adapters (cache.inmemory, persistence.\*,
  secrets.dapr, observability.otel, migrations.dbup) under the Adapters section and documents
  relocated features as API-layer concerns
- AC-08: Test count before and after refactor is identical (no tests lost)
- AC-09: `docs/authentication.md` links point to the correct new locations

### Notes

- Must be delivered last (after US-01 through US-04)
- This is the coordinated release point — all 5 stories ship together
- US-01, US-02, US-03, US-04 completed (source files already relocated)
- Namespace changes may cause test compilation failures — mitigation: bulk find-and-replace with build verification;
    owner: 3M0R4C
- Solution filter files may have undocumented dependencies — mitigation: validate with `dotnet build` on each filter;
    owner: 3M0R4C

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-27`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No changes — domain layer is unaffected by test relocation
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No changes — application contracts remain intact
  - Potential files/folders to touch: `None`
- Database Schema
  - Change summary: No changes — no migrations required
  - Potential files/folders to touch: `None`
- API
  - Change summary: No changes to HTTP surface or production code
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch: `None`
- Adapters
  - Change summary: No production adapter code changes; remove adapter READMEs for the 4 relocated features (rate
    limiting, JWT, API Key, Swagger) since those features now live in the API layer per US-01 through US-04
  - Potential files/folders to touch: `src/Adapters/emc.camus.ratelimiting.inmemory/README.md`,
    `src/Adapters/emc.camus.security.jwt/README.md`, `src/Adapters/emc.camus.security.apikey/README.md`,
    `src/Adapters/emc.camus.documentation.swagger/README.md`
- Tests
  - Change summary: Relocate all unit tests from the 4 orphaned test projects into `emc.camus.api.test` using
    feature-aligned subdirectories (RateLimiting, Security/Jwt, Security/ApiKey, Documentation); relocate any
    integration tests into `emc.camus.api.integration.test` with matching subdirectories; update namespaces to match the
    target project namespace hierarchy; add necessary package and project references to the receiving test projects;
    remove the 4 orphaned test projects from the solution and solution filters; delete the orphaned directories
  - Potential files/folders to touch: `src/Test/emc.camus.api.test/`, `src/Test/emc.camus.api.integration.test/`,
    `src/Test/emc.camus.ratelimiting.inmemory.test/` (delete), `src/Test/emc.camus.security.jwt.test/` (delete),
    `src/Test/emc.camus.security.apikey.test/` (delete), `src/Test/emc.camus.documentation.swagger.test/` (delete),
    `src/CamusApp.sln`, `src/UnitTests.slnf`, `src/IntegrationTests.slnf`
- Documentation
  - Change summary: Update `README.md` project structure section to remove the 4 orphaned test projects and the 4
    relocated adapter directories; update `docs/architecture.md` Adapters list to reflect only true adapters
    (cache.inmemory, persistence.\*, secrets.dapr, observability.otel, migrations.dbup) and document relocated features
    as API-layer concerns; verify `docs/authentication.md` links point to correct locations; remove or archive adapter
    READMEs for the 4 relocated features
  - Potential files/folders to touch: `README.md`, `docs/architecture.md`, `docs/authentication.md`

### Cross-Cutting Concern Decisions

- Reliability: Validate test count before and after relocation is identical using `dotnet test --list-tests` counts; run
  full solution test suite (`dotnet test src/CamusApp.sln`) as the final gate to confirm zero regressions; validate each
  solution filter compiles independently with `dotnet build`
- Rollout: Single coordinated commit delivered after US-01 through US-04 are complete; all 5 stories ship
  together as one atomic release
- Rollback: Revert the single commit — since this story only moves test files, updates documentation, and
  removes orphaned projects, a git revert fully restores the previous state with no data or runtime implications
- Operational readiness: Not applicable — no runtime behavior changes; CI pipeline confirms all tests pass
  post-merge

### Architect Handoff Gate

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `3M0R4C, 2026-05-27`

## Section C - Implementation Tracking

Unit-test phase bypassed: this story is a pure refactoring/relocation task with no new production behavior to drive
via TDD. The Layer Impact Matrix specifies zero new classes, interfaces, or methods; acceptance criteria are
structural or documentation checks verified through build/test commands and documentation review (AC-01 via
`dotnet test src/CamusApp.sln`; AC-02 via grep against `.sln`/`.slnf`; AC-03/AC-04 via documentation review;
AC-05 via `dotnet test --list-tests` count comparison; AC-06 via link validation).

### Test Traceability

| AC | Test Class | Test Method | Layer | Change |
| --- | --- | --- | --- | --- |
| N/A | — | — | — | — |

> No TDD unit tests required. Acceptance criteria are verified through build/test commands and documentation review.

### Skeleton Inventory

| Layer | Stub File | Change | Types | Members |
| --- | --- | --- | --- | --- |
| N/A | — | — | — | — |

> No production stubs required. This story creates no new types or behavioral methods.

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `N/A — bypassed (structural refactor, no testable behavior)`
- Skeleton inventory complete and user-approved: `N/A — no stubs needed`
- Tests compile and fail for the right reason (TDD red): `N/A — bypassed`
- Ready for developer implementation: `Yes`
- Tester sign-off: `3M0R4C, 2026-05-29`

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
| 1 | RateLimitHeadersMiddlewareTests.cs | All methods | Added `using emc.camus.api.Configurations;` | `RateLimitContextKeys` moved to API Configurations namespace |
| 2 | ApiKeyAuthenticationHandler.cs | N/A (production) | Replaced `AuthenticationSchemes.ApiKey` with `ApiKeySettings.AuthenticationScheme` | `AuthenticationSchemes` moved to API.Configurations; adapter cannot reference API |
| 3 | ApiKeySetupExtensions.cs | N/A (production) | Replaced `AuthenticationSchemes.ApiKey` with `ApiKeySettings.AuthenticationScheme` | Same as above |
| 4 | Program.cs | N/A (production) | Switched to API-internal `AddJwtAuthentication()` and `AddApiKeyAuth()` | Adapters deleted; API owns authentication wiring |
| 5 | emc.camus.api.csproj | N/A (production) | Added JWT NuGet packages, removed adapter ProjectReferences | Packages were transitive via deleted adapters |
| 6 | JwtSetupExtensions.cs | N/A (production) | Converted `AddJwtAuthenticationInternal` to extension method `AddJwtAuthentication` | Adapter no longer present; no ambiguity |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `Yes`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-05-29`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| API → RateLimiting middleware → HTTP response | ApiRateLimitingFactory | RateLimitingIpPartitionTests | (all existing methods) | Existing |
| API → JWT authentication → Token generation/validation | ApiInMemoryFactory | AuthInMemoryEndpointTests | (all existing methods) | Existing |
| API → JWT authentication → PostgreSQL persistence | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | (all existing methods) | Existing |
| API → Token revocation cache → PostgreSQL | ApiPostgreSqlFactory | TokenRevocationCachePostgreSqlTests | (all existing methods) | Existing |
| API → Swagger documentation → HTTP response | ApiInMemoryFactory | SwaggerDocumentationInMemoryTests | (all existing methods) | Existing |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

> No findings. All 63 integration tests pass without modification.

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `3M0R4C, 2026-05-29`
