# User Story Specification

## Metadata

- Story ID: `US-05`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `consolidate-tests-and-docs`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want
`the tests from relocated adapter projects consolidated into emc.camus.api.test and
emc.camus.api.integration.test, and all affected documentation updated`, so that
`the test structure mirrors the new project layout and documentation accurately reflects the
architectural boundary between API features and adapters`.

### Business Value

- Test projects mirror production projects 1:1 — no orphaned test projects referencing deleted adapters
- Documentation accurately guides developers and reduces onboarding confusion
- Solution file is clean with no dangling references
- 100% test coverage is preserved across the relocation

### In Scope

- Move tests from `src/Test/emc.camus.ratelimiting.inmemory.test/` into `src/Test/emc.camus.api.test/`
- Move tests from `src/Test/emc.camus.security.jwt.test/` into `src/Test/emc.camus.api.test/`
- Move tests from `src/Test/emc.camus.security.apikey.test/` into `src/Test/emc.camus.api.test/`
- Move tests from `src/Test/emc.camus.documentation.swagger.test/` into `src/Test/emc.camus.api.test/`
- Move any integration tests from the above into `src/Test/emc.camus.api.integration.test/`
- Remove the 4 orphaned test projects from the solution
- Delete the 4 orphaned test project directories
- Update solution filters (`UnitTests.slnf`, `IntegrationTests.slnf`) to reflect changes
- Update `README.md` project structure section
- Update `docs/architecture.md` to reflect the new boundary
- Update or remove affected adapter READMEs (rate limiting, JWT, API Key, Swagger)
- Adjust namespace references in relocated tests as needed
- Ensure 100% of existing tests pass with adjustments where needed

### Out of Scope

- Writing new tests beyond what's needed to maintain coverage
- Changing test assertions or test behavior
- Modifying true adapter READMEs (cache.inmemory, persistence.\*, secrets.dapr, observability.otel, migrations.dbup)
- Changing CI/CD pipeline configuration (if any)

### Functional Requirements

- FR-01: All unit tests from the 4 removed adapter test projects exist in `src/Test/emc.camus.api.test/` with
  appropriate subdirectory organization
- FR-02: All integration tests from the 4 removed adapter test projects exist in
  `src/Test/emc.camus.api.integration.test/`
- FR-03: The 4 orphaned test projects are removed from the solution and their directories deleted
- FR-04: Solution filters (`UnitTests.slnf`, `IntegrationTests.slnf`) reference only existing projects
- FR-05: `README.md` project structure section accurately reflects the new layout
- FR-06: `docs/architecture.md` accurately describes the boundary between API features and true adapters
- FR-07: Adapter READMEs for the 4 relocated features are removed or archived

### Non-Functional Requirements

- Security: N/A
- Performance: N/A
- Observability: N/A
- Reliability: All existing tests pass — 100% coverage preserved with adjustments where needed
- Compliance: N/A

### Acceptance Criteria

- AC-01: `dotnet test src/CamusApp.sln` passes with zero failures
- AC-02: No references to `emc.camus.ratelimiting.inmemory`, `emc.camus.security.jwt`, `emc.camus.security.apikey`, or
  `emc.camus.documentation.swagger` exist in `.sln` or `.slnf` files
- AC-03: `README.md` project structure matches the actual directory layout
- AC-04: `docs/architecture.md` lists only true adapters (cache.inmemory, persistence.\*, secrets.dapr,
  observability.otel, migrations.dbup) under the Adapters section and documents relocated features as API-layer concerns
- AC-05: Test count before and after refactor is identical (no tests lost)
- AC-06: `docs/authentication.md` links point to the correct new locations

### Constraints and Dependencies

- Business constraints:
  - Must be delivered last (after US-01 through US-04)
  - This is the coordinated release point — all 5 stories ship together
- Dependencies:
  - US-01, US-02, US-03, US-04 completed (source files already relocated)

### Risks and Open Questions

- Risks:
  - Namespace changes may cause test compilation failures — mitigation: bulk find-and-replace with build verification;
    owner: Tech Lead
  - Solution filter files may have undocumented dependencies — mitigation: validate with `dotnet build` on each filter;
    owner: Tech Lead
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

### Delivery and Rollout Notes

- Rollout strategy: Single coordinated commit delivered after US-01 through US-04 are complete; all 5 stories ship
  together as one atomic release
- Rollback strategy: Revert the single commit — since this story only moves test files, updates documentation, and
  removes orphaned projects, a git revert fully restores the previous state with no data or runtime implications
- Operational readiness checks: Not applicable — no runtime behavior changes; CI pipeline confirms all tests pass
  post-merge

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

| AC    | Test Class      | Test Method                          | Layer                               | Change          |
| ----- | --------------- | ------------------------------------ | ----------------------------------- | --------------- |
| AC-01 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-02 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |
| AC-03 | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [Domain, Application, Api, Adapter] | [New, Modified] |

### Skeleton Inventory

| Layer                               | Stub File             | Change          | Types                      | Members                         |
| ----------------------------------- | --------------------- | --------------- | -------------------------- | ------------------------------- |
| [Domain, Application, Api, Adapter] | [src/.../FileName.cs] | [New, Modified] | [class, interface, record] | [method signatures, properties] |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `[Yes | No]`
- Skeleton inventory complete and user-approved: `[Yes | No]`
- Tests compile and fail for the right reason (TDD red): `[Yes | No]`
- Ready for implementation: `[Yes | No]`
- Tester sign-off: `[Name, Date]`

### Regression Fixes Log

| #   | Test File        | Test Method   | Change Made          | Reason                                  |
| --- | ---------------- | ------------- | -------------------- | --------------------------------------- |
| [n] | [test file path] | [method name] | [description of fix] | [contract change that caused the break] |

### Developer Handoff Gate

- All unit tests pass (TDD green): `[Yes | No]`
- All existing integration tests pass: `[Yes | No]`
- Regression fixes documented (if any): `[Yes | N/A]`
- Build succeeds with zero warnings: `[Yes | No]`
- Ready for code review: `[Yes | No]`
- Developer sign-off: `[Name, Date]`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary               | Factory              | Test Class      | Test Method                          | Change                    |
| ---------------------- | -------------------- | --------------- | ------------------------------------ | ------------------------- |
| [cross-layer boundary] | [factory class name] | [TestClassName] | [MethodName_Scenario_ExpectedResult] | [New, Modified, Existing] |

### Integration Test Findings

| #   | Test          | Failure               | Root Cause Analysis | Affected File          |
| --- | ------------- | --------------------- | ------------------- | ---------------------- |
| [n] | [test method] | [failure description] | [analysis]          | [production file path] |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `[Yes | No]`
- All integration tests pass: `[Yes | No]`
- No unresolved production code findings: `[Yes | No]`
- Ready for review: `[Yes | No]`
- Integration Tester sign-off: `[Name, Date]`

## Section E - Technical Writer

### Version Update

- Previous version: `[X.X.X]`
- New version: `[X.X.X]`
- Bump type: `[MAJOR | MINOR | PATCH | APPEND]`
- Reason: `[one-sentence justification]`

### CHANGELOG Entry
