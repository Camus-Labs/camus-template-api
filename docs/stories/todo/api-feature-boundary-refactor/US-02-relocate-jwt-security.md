# User Story Specification

## Metadata

- Story ID: `US-02`
- Feature Slug: `api-feature-boundary-refactor`
- Story Slug: `relocate-jwt-security`
- Request Date: `2026-05-27`
- Requested By: `Tech Lead`

## Section A - Product Owner Definition

### Story Statement

As a `platform maintainer`, I want `the JWT security feature relocated from Adapters into the API
layer`, so that `the architectural boundary correctly reflects that JWT authentication is an ASP.NET
Core pipeline feature, not a swappable infrastructure adapter behind an Application port`.

### Business Value

- Clarifies hexagonal architecture: JWT handler is middleware/auth-scheme wiring, not an adapter implementing a domain
  port
- Consolidates all authentication pipeline configuration in one project for easier reasoning
- Maintains secrets consumption through the true adapter (`emc.camus.secrets.dapr`) and cache consumption through
  `emc.camus.cache.inmemory`

### In Scope

- Move all source files from `src/Adapters/emc.camus.security.jwt/` into `src/Api/emc.camus.api/` following the
  Configurations/ + Middleware|Handlers/ + Extensions/\*SetupExtensions.cs + Metrics/ pattern
- Update `emc.camus.api.csproj` to remove the project reference to `emc.camus.security.jwt`
- Remove the `emc.camus.security.jwt` project from the solution
- Delete the `src/Adapters/emc.camus.security.jwt/` directory
- Preserve consumption of `emc.camus.secrets.dapr` for RSA key retrieval
- Preserve consumption of `emc.camus.cache.inmemory` for token revocation cache
- Update `Program.cs` composition root to wire JWT from the API project directly
- Maintain identical HTTP behavior: same 401/403 responses, same claims, same token validation

### Out of Scope

- Changing JWT algorithms, claims structure, or token lifetime logic
- Modifying the Application-layer `Auth/` contracts
- Modifying `emc.camus.secrets.dapr` or `emc.camus.cache.inmemory` adapters
- Test relocation (covered in US-05)

### Functional Requirements

- FR-01: All JWT security source files are located under `src/Api/emc.camus.api/` in appropriate subdirectories
- FR-02: The `emc.camus.security.jwt` project is removed from the solution and its directory deleted
- FR-03: The API project no longer holds a project reference to `emc.camus.security.jwt`
- FR-04: JWT DI registration occurs via an extension method in `src/Api/emc.camus.api/Extensions/`
- FR-05: JWT feature continues consuming `ISecretProvider` (from `emc.camus.secrets.dapr`) for RSA key retrieval
- FR-06: JWT feature continues consuming the cache port (from `emc.camus.cache.inmemory`) for token revocation

### Non-Functional Requirements

- Security: No change — JWT Bearer authentication continues protecting endpoints identically
- Performance: No performance considerations
- Observability: Existing JWT/auth metrics remain functional with same metric names
- Reliability: Single coordinated release; no intermediate broken state
- Compliance: N/A

### Acceptance Criteria

- AC-01: Solution builds successfully without `emc.camus.security.jwt` project reference
- AC-02: All JWT security source files exist under `src/Api/emc.camus.api/` in the correct subdirectories
- AC-03: JWT authentication validates tokens identically (same issuer, audience, RSA validation, expiration checks)
- AC-04: Token revocation cache integration works identically (revoked tokens are rejected)
- AC-05: HTTP responses for auth failures return identical status codes (401/403) and response bodies

### Constraints and Dependencies

- Business constraints:
  - Must be delivered after US-01 (sequential ordering)
  - Single coordinated release with US-05
- Dependencies:
  - `emc.camus.secrets.dapr` adapter remains unchanged and available
  - `emc.camus.cache.inmemory` adapter remains unchanged and available
  - Application-layer `Auth/` contracts remain unchanged

### Risks and Open Questions

- Risks:
  - JWT handler may have NuGet dependencies that inflate the API project — mitigation: review package references before
    move; owner: Tech Lead
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
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- Application
  - Change summary: No changes — existing `Auth/` contracts (`ITokenGenerator`, `ITokenRevocationCache`,
    `ISecretProvider`) remain unchanged and continue to be implemented by code now hosted in the API project
  - Potential files/folders to touch: `None`
- Database Schema
  - Change summary: No changes
  - Potential files/folders to touch: `None`
- API
  - Change summary: Absorb all JWT security source files from the adapter into the API project, following existing API
    conventions; add the adapter's NuGet packages to the API `.csproj`; remove the project reference to
    `emc.camus.security.jwt`; update `Program.cs` composition root to call the relocated extension method directly
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch:
    - `src/Api/emc.camus.api/emc.camus.api.csproj` — add JWT NuGet packages, remove adapter project reference
    - `src/Api/emc.camus.api/Extensions/JwtSetupExtensions.cs` — relocated setup extension
    - `src/Api/emc.camus.api/Configurations/JwtSettings.cs` — relocated settings class
    - `src/Api/emc.camus.api/Infrastructure/JwtTokenGenerator.cs` — relocated token generator service
    - `src/Api/emc.camus.api/Infrastructure/JwtKeyLoadException.cs` — relocated exception
    - `src/Api/emc.camus.api/Infrastructure/JwtTokenGenerationException.cs` — relocated exception
    - `src/Api/emc.camus.api/Program.cs` — update composition root call-site namespace
- Adapters
  - Change summary: Remove the `emc.camus.security.jwt` project entirely from the solution and delete its directory; no
    other adapters are modified
  - Potential files/folders to touch:
    - `src/Adapters/emc.camus.security.jwt/` — entire directory deleted
    - `src/CamusApp.sln` — remove project entry
- Tests
  - Change summary: Out of scope for this story (covered in US-05); existing unit tests in `emc.camus.security.jwt.test`
    will be relocated by US-05
  - Potential files/folders to touch: `None (deferred to US-05)`

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: No behavioral change — JWT Bearer authentication scheme, RSA256 validation, claims mapping, revocation
  check, and 401/403 responses remain identical; only the hosting project changes. The `ISecretProvider` dependency
  continues to supply RSA keys and the `ITokenRevocationCache` continues to supply revocation state.
- Performance: No impact — the same singleton registrations and token validation pipeline execute at runtime regardless
  of which project hosts the source files.
- Observability: Existing JWT/auth metrics (metric names and dimensions) remain unchanged; the relocated code preserves
  all instrumentation call-sites. No metric rename or re-registration required.
- Reliability: Delivered as a single atomic commit; the solution must build and pass all tests at every commit boundary.
  Coordinated release with US-05 (test relocation) ensures no dangling project references.
- Compliance: N/A — no data handling or regulatory changes.

### Delivery and Rollout Notes

- Rollout strategy: Full rollout in a single coordinated release alongside US-05 (test relocation). No feature flag
  needed — this is a pure internal restructuring with no external behavior change.
- Rollback strategy: Revert the merge commit to restore the `emc.camus.security.jwt` project reference and source files;
  the solution returns to its pre-refactor state with no data migration or state cleanup required.
- Operational readiness checks: Verify solution builds without `emc.camus.security.jwt`; verify JWT authentication
  end-to-end (token generation, validation, revocation rejection, 401/403 responses); verify existing auth metric
  dashboards continue to populate.

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

| AC    | Test Class             | Test Method                                                                         | Layer | Change |
| ----- | ---------------------- | ----------------------------------------------------------------------------------- | ----- | ------ |
| AC-01 | JwtSettingsTests       | Constructor_Defaults_HasExpectedValues                                              | Api   | New    |
| AC-02 | JwtSettingsTests       | Validate_IssuerAtMaxLength_DoesNotThrow                                             | Api   | New    |
| AC-02 | JwtSettingsTests       | Validate_AudienceAtMaxLength_DoesNotThrow                                           | Api   | New    |
| AC-02 | JwtSettingsTests       | Validate_ExpirationMinutesAtBoundary_DoesNotThrow                                   | Api   | New    |
| AC-02 | JwtSettingsTests       | Validate_RsaPrivateKeySecretNameAtMaxLength_DoesNotThrow                            | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_NullOrEmptyIssuer_ThrowsInvalidOperationException                          | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_IssuerExceedingMaxLength_ThrowsInvalidOperationException                   | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_IssuerNotValidUrl_ThrowsInvalidOperationException                          | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_NullOrEmptyAudience_ThrowsInvalidOperationException                        | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_AudienceExceedingMaxLength_ThrowsInvalidOperationException                 | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_AudienceNotValidUrl_ThrowsInvalidOperationException                        | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_ExpirationMinutesOutOfRange_ThrowsInvalidOperationException                | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_NullOrEmptyRsaPrivateKeySecretName_ThrowsInvalidOperationException         | Api   | New    |
| AC-03 | JwtSettingsTests       | Validate_RsaPrivateKeySecretNameExceedingMaxLength_ThrowsInvalidOperationException  | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | Constructor_NullJwtSettings_ThrowsArgumentNullException                             | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | Constructor_NullSigningCredentials_ThrowsArgumentNullException                      | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | Constructor_NullTimeProvider_ThrowsArgumentNullException                            | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_EmptyUserId_ThrowsArgumentOutOfRangeException                         | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_NullOrWhiteSpaceUsername_ThrowsArgumentException                      | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_ValidInputs_ReturnsTokenWithExpectedProperties                        | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithAdditionalClaims_TokenContainsAdditionalClaims                    | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithNullAdditionalClaims_DoesNotThrow                                 | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_EmptyUserId_ThrowsArgumentOutOfRangeException                 | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_NullOrWhiteSpaceUsername_ThrowsArgumentException              | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_EmptyJti_ThrowsArgumentOutOfRangeException                    | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_DefaultExpiresOn_ThrowsArgumentOutOfRangeException            | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_ValidInputs_ReturnsTokenWithExpectedProperties                | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_WithAdditionalClaims_TokenContainsAdditionalClaims            | Api   | New    |
| AC-03 | JwtTokenGeneratorTests | GenerateToken_WithJti_NullAdditionalClaims_DoesNotThrow                             | Api   | New    |
| AC-04 | JwtTokenGeneratorTests | GenerateToken_ValidInputs_ReturnsTokenWithExpectedProperties                        | Api   | New    |
| AC-05 | JwtTokenGeneratorTests | GenerateToken_SigningFailure_ThrowsJwtTokenGenerationException                      | Api   | New    |

### Skeleton Inventory

| Layer | Stub File                                                                 | Change | Types                                            | Members                                                                                              |
| ----- | ------------------------------------------------------------------------- | ------ | ------------------------------------------------ | ---------------------------------------------------------------------------------------------------- |
| Api   | src/Api/emc.camus.api/Configurations/JwtSettings.cs                       | New    | public sealed class JwtSettings                  | Issuer, Audience, ExpirationMinutes, RsaPrivateKeySecretName, Validate()                             |
| Api   | src/Api/emc.camus.api/Infrastructure/JwtTokenGenerator.cs                 | New    | public sealed class JwtTokenGenerator            | GenerateToken(Guid, string, IEnumerable?), GenerateToken(Guid, string, Guid, DateTime, IEnumerable?) |
| Api   | src/Api/emc.camus.api/Infrastructure/JwtKeyLoadException.cs               | New    | public sealed class JwtKeyLoadException          | ctor(string, Exception)                                                                              |
| Api   | src/Api/emc.camus.api/Infrastructure/JwtTokenGenerationException.cs       | New    | public sealed class JwtTokenGenerationException  | ctor(string, Exception)                                                                              |
| Api   | src/Api/emc.camus.api/Extensions/JwtSetupExtensions.cs                    | New    | public static class JwtSetupExtensions           | AddJwtAuthenticationInternal(WebApplicationBuilder)                                                  |

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `Yes`
- Skeleton inventory complete and user-approved: `Yes`
- Tests compile and fail for the right reason (TDD red): `Yes`
- Ready for implementation: `Yes`
- Tester sign-off: `3M0R4C, 2026-05-28`

### Regression Fixes Log

| #   | Test File        | Test Method   | Change Made          | Reason                                  |
| --- | ---------------- | ------------- | -------------------- | --------------------------------------- |
| —   | —                | —             | —                    | No regressions                          |

### Developer Handoff Gate

- All unit tests pass (TDD green): `Yes`
- All existing integration tests pass: `Yes`
- Regression fixes documented (if any): `N/A`
- Build succeeds with zero warnings: `Yes`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-05-28`

## Section D - Integration Testing

### Integration Test Traceability

| Boundary | Factory | Test Class | Test Method | Change |
| --- | --- | --- | --- | --- |
| API→Application (JWT token generation via ITokenGenerator) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_ValidAdminCredentials_ReturnsOkWithToken | Existing |
| API→Application (JWT token generation with persistence) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | GenerateToken_ValidRequest_ReturnsCreatedWithPersistedToken | Existing |
| API→Application (JWT token validation pipeline — invalid credentials) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_InvalidPassword_ReturnsUnauthorized | Existing |
| API→Application (JWT token validation pipeline — missing API key) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_NoApiKey_ReturnsUnauthorized | Existing |
| API→Cache (Token revocation check in JWT validation) | ApiPostgreSqlFactory | TokenRevocationCachePostgreSqlTests | BackgroundSync_RevokedTokenInDatabase_CacheRejectsTokenAfterSyncCycle | Existing |
| API→Secrets (RSA key loading via ISecretProvider) | ApiInMemoryFactory | AuthInMemoryEndpointTests | Authenticate_ValidAdminCredentials_ReturnsOkWithToken | Existing |
| API→Persistence (Token revocation with DB update) | ApiPostgreSqlFactory | AuthPostgreSqlEndpointTests | RevokeToken_ValidJti_ReturnsOkWithRevokedToken | Existing |

### Integration Test Findings

| # | Test | Failure | Root Cause Analysis | Affected File |
| --- | --- | --- | --- | --- |
| — | — | — | — | No findings — all 61 integration tests pass |

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `Yes`
- All integration tests pass: `Yes`
- No unresolved production code findings: `Yes`
- Ready for review: `Yes`
- Integration Tester sign-off: `3M0R4C, 2026-05-28`

## Section E - Technical Writer

### Status

`DOCUMENTED`

### Version Update

- Previous version: `1.0.1`
- New version: `1.0.1`
- Bump type: `APPEND`
- Reason: `Appending to existing 1.0.1 release as part of same api-feature-boundary-refactor feature delivery`

### CHANGELOG Entry

```markdown
### Changed

- Relocate JWT security feature from Adapters layer into the API layer to clarify architectural boundaries

### Removed

- Remove `emc.camus.security.jwt` adapter project from the solution
```

### Documentation Updates

- Swagger annotations updated: 0 endpoint(s)
- Postman requests updated: 0 request(s)
- Files modified: `CHANGELOG.md`, `docs/stories/todo/api-feature-boundary-refactor/US-02-relocate-jwt-security.md`

### Technical Writer Handoff Gate

- Version in Directory.Build.props matches confirmed decision: `Yes`
- CHANGELOG entry matches new version and date: `Yes`
- Swagger examples reflect new/changed endpoints: `N/A`
- Postman collection reflects new/changed requests: `N/A`
- Markdown linting passes with zero errors: `Yes`
- Build succeeds with zero errors and warnings: `Yes`
- Technical Writer sign-off: `3M0R4C, 2026-05-28`
