# Pre-Commit Feature Review Checklist

This checklist ensures new features follow architectural principles, avoid over-engineering, and maintain documentation consistency.

**Usage**: Replace `[FEATURE_NAME]` with the actual feature (e.g., "rate limiting", "JWT authentication"). Execute sections in order: Architecture → Observability → Documentation.

---

## A. ARCHITECTURE REVIEW

Find and review all classes, enums, interfaces, doc and other files related to [FEATURE_NAME] to check them against:

### Architecture Checklist

#### Clean/Hexagonal Compliance

- [ ] Application layer does NOT depend on infrastructure (violation if true)
- [ ] Abstractions (interfaces/exceptions) in Application are used by API or multiple adapters
- [ ] Adapter-specific interfaces are kept in adapters (correct placement)
- [ ] Dependency direction follows: API/Adapters → Application → Domain (never reverse)

#### Layer Placement Validation

##### Application Layer (Shared Contracts)

**Purpose**: Defines contracts (interfaces, attributes, exceptions) that decouple API from infrastructure adapters. **NOT for business logic** - that goes in Domain.

**✅ ALLOWED (DO):**

- [ ] Custom attributes for cross-cutting concerns (e.g., `[RateLimit]`)
- [ ] Custom exceptions for infrastructure failures (e.g., `RateLimitExceededException`, `AuthenticationFailedException`)
- [ ] Port interfaces for adapters with multiple implementations (e.g., `ITokenGenerator`, `IUserRepository`, `ISecretProvider`)
- [ ] Interfaces consumed by multiple adapters (shared contracts)
- [ ] Constants for cross-cutting concerns (e.g., `ErrorCodes.RateLimitExceeded`, `Headers.ApiKey`, `Headers.TraceId`)
- [ ] Application services as concrete classes (e.g., `AuthService`) - no interface needed unless multiple implementations exist
- [ ] **CQRS-style type organization** per feature folder (e.g., `Auth/`):
  - `*Commands.cs` — positional records for write inputs (e.g., `AuthenticateUserCommand`, `GenerateTokenCommand`)
  - `*Results.cs` — positional records for write outputs **only when no existing View matches the shape** (e.g., `AuthenticateUserResult` returns a JWT token string that no View would expose)
  - `*Filters.cs` — positional records with defaults for query inputs (e.g., `GeneratedTokenFilter(bool ExcludeRevoked = false)`)
  - `*Views.cs` — positional records for query output projections (e.g., `GeneratedTokenSummaryView`)
- [ ] **Views are the default return type** for both queries and commands. Commands that return the entity's current state after mutation should reuse an existing View (e.g., `RevokeTokenAsync` returns `GeneratedTokenSummaryView`). Create a dedicated Result only when the command output has a genuinely different shape (e.g., `GenerateTokenAsync` returns `GenerateTokenResult` because it includes the raw JWT token, which no View exposes)
- [ ] **View naming convention**: Views describe **content/shape**, not the query method — e.g., `GeneratedTokenSummaryView` (correct) vs `GeneratedTokenByUserView` (wrong: encodes query, not shape)
- [ ] **Filter naming convention**: Filters target the **entity**, not a specific view — e.g., `GeneratedTokenFilter` (correct) vs `GeneratedTokenSummaryFilter` (wrong: ties filter to one view shape)
- [ ] **Common types** in `Common/` folder for shared application concerns:
  - `PaginationParams` — constructor-validated value object with `get`-only properties (no `init` setters that bypass validation)
  - `PagedResult<T>` — generic paginated result container (query output wrapper, not a domain concept)
- [ ] **Constructor-based validation** for value objects (e.g., `PaginationParams`) — constructor enforces invariants via `Math.Max`/`Math.Clamp`, properties are `get`-only. No factory methods with `init` setters (backdoor bypasses validation)
- [ ] Application services use **private mapping helpers** (e.g., `ToSummaryView`) for entity → view conversion to avoid duplication across methods

**❌ FORBIDDEN (DON'T):**

- [ ] HTTP runtime objects (HttpContext, HttpRequest, HttpResponse)
- [ ] Infrastructure implementations (database, file I/O, caching, secrets)
- [ ] Configuration classes (Settings, Options)
- [ ] Interfaces for single-implementation application services (YAGNI - add when second implementation is needed)
- [ ] Middleware or DI registration
- [ ] Business/domain logic
- [ ] Factory methods with `init` setters on value objects — use constructor-based validation instead
- [ ] Unbounded list queries for **growing datasets** without pagination — use `PagedResult<T>` with `PaginationParams`. Unbounded lists are acceptable for small, naturally bounded datasets (e.g., API versions, permission types, enum-like reference data)

##### Domain Layer (Business Logic)

**Purpose**: Pure business entities, rules, and logic. **No dependencies** on any other layer or infrastructure.

- [ ] Business entities with **private setters** on all properties (e.g., `User`, `Order`, `ApiInfo`, `Credentials`)
- [ ] Value objects (e.g., `Email`, `Money`, `Address`)
- [ ] Domain rules and business validation (e.g., "age >= 18", "price > 0", "email format valid")
- [ ] Domain exceptions for business rule violations (e.g., `InvalidAgeException`, `PriceCannotBeNegativeException`)
- [ ] Domain constants (business-related, e.g., `MinimumAge = 18`)
- [ ] Domain events (if using event-driven patterns)
- [ ] Extension methods for domain entities (e.g., `user.IsAdult()`, `order.CalculateTotal()`)
- [ ] **Public constructor** for creating new entities — validates all business attributes, sets initial state (e.g., `IsRevoked = false`), auto-generates ID when null
- [ ] **`Reconstitute` static factory** for rebuilding entities from persistence — accepts all fields (including lifecycle fields), skips business validation, returns fully hydrated entity
- [ ] **Business methods** for state transitions (e.g., `Revoke()`, `Activate()`, `Cancel()`) — enforce invariants, the ONLY way to mutate entity state after construction
- [ ] **Lifecycle/audit fields** (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) are **read-only** — populated only via `Reconstitute` on read paths, never set by constructors or business methods (repository concerns)
- [ ] No infrastructure dependencies (database, HTTP, file system), Application layer references, or framework-specific code
- [ ] No `DateTime.UtcNow` or clock dependencies — timestamps are infrastructure concerns

##### API Layer (HTTP Orchestration)

**Purpose**: HTTP pipeline, routing, and web infrastructure configuration. Orchestrates calls to Application interfaces.

**✅ ALLOWED (DO):**

- [ ] Controllers and endpoint handlers
- [ ] DTOs for HTTP translation — kept separate from Application views as a versioning-ready contract boundary
- [ ] **DTO folder conventions**: `Models/Dtos/` for item DTOs, `Models/Responses/` for envelopes (`ApiResponse<T>`, `PagedResponse<T>`), `Models/Requests/` for input models
- [ ] Model binding attributes only (`[FromBody]`, `[FromQuery]`, `[FromRoute]`) - NO validation attributes
- [ ] Middleware and HTTP pipeline configuration (components, ordering, request processing)
- [ ] Action filters and exception filters (`IActionFilter`, `IExceptionFilter`)
- [ ] Dependency injection and service registration configuration (Program.cs, calling adapter extension methods)
- [ ] Error handling, response mapping, and web infrastructure configuration (security policies, cross-cutting concerns)
- [ ] **Mapper extensions** in `Mapping/` folder, split by concern:
  - Feature-specific mappers (`AuthMappingExtensions`) — `ToCommand()`, `ToResponse()`, `ToDto()`, `ToFilter()` conversions between DTOs ↔ Commands/Results/Views
  - Reusable mappers (`CommonMappingExtensions`) — `ToPaginationParams()`, `ToPagedResponse()` shared across features
- [ ] `[ProducesResponseType]` for SUCCESS responses only (200, 201, 204) with typed payloads

**❌ FORBIDDEN (DON'T):**

- [ ] Business/domain logic (calculations, rules, validation)
- [ ] Infrastructure implementations (database, secrets, caching)
- [ ] Using domain entities directly (map to DTOs in controllers)
- [ ] Returning Application views directly without DTO mapping — DTOs are the API contract, views are the application contract
- [ ] Validation attributes on DTOs (`[Required]`, `[StringLength]`, `[Range]`) - validation goes in mapper extensions
- [ ] `[ProducesResponseType]` for error responses (400, 401, 403, 404, 429, 500) - `DefaultApiResponsesOperationFilter` adds these globally

##### Adapters Layer (Infrastructure Implementations)

**Purpose**: Implements Application interfaces using specific technologies. Each adapter is independent and swappable.

**✅ ALLOWED (DO):**

- [ ] Implementation of Application interfaces
- [ ] Infrastructure and technology-specific code (database, HTTP, file system, caching, Dapr, Redis, JWT, etc.)
- [ ] External service clients and SDKs
- [ ] Adapter-specific interfaces (only consumed within adapter)
- [ ] Adapter-specific configuration classes (JwtSettings, DaprSecretProviderSettings, RateLimitSettings)
- [ ] Extension methods for service registration (AddJwtAuthentication, AddDaprSecrets)
- [ ] Adapter-specific middleware

**❌ FORBIDDEN (DON'T):**

- [ ] Business/domain logic
- [ ] HTTP endpoint definitions
- [ ] Shared interfaces used by API (move to Application)

##### Repository Adapter Conventions (Database Persistence)

**Purpose**: Repository adapters implement Application interfaces for database persistence. They follow specific conventions for data mapping, lifecycle management, and write patterns.

**✅ ALLOWED (DO):**

- [ ] **Database Models** in `Models/` folder with `*Model` suffix and PascalCase properties (e.g., `UserModel`, `RoleModel`) — used by Dapper for ORM mapping from database rows
- [ ] **Mapping Extensions** in `Mapping/` folder with `*MappingExtensions` suffix (e.g., `UserMappingExtensions`) — `ToEntity()` extension methods using `Entity.Reconstitute()` factory to rebuild Domain entities from persistence
- [ ] Each Model has a corresponding MappingExtensions class
- [ ] **Lifecycle/audit fields** owned by repository: `created_at` via database `DEFAULT NOW()`, `updated_at` set on UPDATE queries, `created_by`/`updated_by` received as parameter from application service (which gets it from `IUserContext`)
- [ ] **Entity-centric writes** for entities with state transitions and business invariants (e.g., `GeneratedToken` with `Revoke()`, `Order` with `Cancel()`) — application service constructs/mutates entity, repository persists it (load → mutate → save)
- [ ] **Parameter-based writes** for infrastructure operations without domain behavior (e.g., `AuthenticateAsync(username, password)` where BCrypt is an infrastructure concern) — repository encapsulates infrastructure-specific logic
- [ ] Entity-centric `Create` accepts domain entity — repository extracts fields for INSERT and owns lifecycle defaults (`created_at`, `created_by`)
- [ ] Entity-centric `Update/Save` accepts domain entity — repository persists mutated state and owns lifecycle updates (`updated_at`, `updated_by`)
- [ ] Dapper maps database rows to `*Model` classes, `*MappingExtensions.ToEntity()` rebuilds Domain entities via `Reconstitute()`, repository methods return Domain entities (not Models)
- [ ] **Paginated queries** preferred over unbounded list queries — use `PagedResult<T>` with `PaginationParams` and SQL `LIMIT/OFFSET`. Pagination and sorting always done server-side (database), not client-side
- [ ] **Filters** (e.g., `GeneratedTokenFilter`) passed from service to repository for dynamic `WHERE` clause construction using `DynamicParameters`
- [ ] Repository read methods return **domain entities** — the application service maps entities to Views (not the repository)

**❌ FORBIDDEN (DON'T):**

- [ ] Inline/nested/private DTO classes inside repositories — use `Models/` folder with `*Model` convention
- [ ] Inline Model-to-Entity mapping inside repositories — use `Mapping/` folder with `*MappingExtensions` convention
- [ ] Duplicating business rules in SQL that already exist in domain entity methods
- [ ] Exposing infrastructure internals (password hashes, encryption keys) to domain or application layers
- [ ] Unbounded list queries for **growing datasets** (e.g., `GetAllTokensAsync()`) — always provide paginated alternatives. Unbounded lists are acceptable for small, naturally bounded datasets (e.g., API versions, roles, permission types)

##### Configuration Classes & Validation Pattern

**Settings classes must follow this pattern:**

- [ ] **Use enums for type-safe options** (not string constants) to prevent configuration errors
  - **Exception**: Use validated strings when referencing framework-mandated identifiers (authentication schemes, content types) or shared application constants used in attributes/middleware. Validate against allowed values with case-insensitive comparison.
- [ ] **Validate enum values** with `Enum.IsDefined()` to catch appsettings.json misconfigurations
  - For string-based settings, validate against a defined list of valid values
- [ ] **Validation logic in settings class** as private `ValidateXxx()` methods called from public `Validate()`
- [ ] **Each property has its own validation method** (e.g., `ValidateExporter()`, `ValidateOtlpEndpoint()`)
- [ ] **Validation constants as private const fields** - validation limits, ranges, and magic values should be declared as `private const` fields
- [ ] **XML exception documentation** on `Validate()` method indicating exception types
- [ ] **No separate validator classes** - keep validation with the data it validates

#### Coding Best Practices - Violations to Fix

- [ ] No duplicate code/logic across files
- [ ] No magic numbers/strings (use constants)
- [ ] Error handling present for expected failures
- [ ] Consistent naming conventions throughout
- [ ] XML documentation on all public APIs

#### Code Quality

- [ ] Validation rules are clear and fail-fast
- [ ] Code follows existing project patterns

#### Exception handling, Validation Pattern and Requests workflow

- [ ] All validation exceptions cascade to ExceptionHandlingMiddleware for standard ProblemDetails output
- [ ] Validation methods throw exceptions (never return null/false for validation failures)
- [ ] Exception messages must be clear, actionable, and match test expectations
- [ ] API Input validates HTTP request format, required fields, data types, basic constraints
- [ ] Controller layer uses Mapper extensions to convert from/to API Request/Response DTOs to/from Application Commands/Results/Views records
- [ ] Application services validate business logic and workflow rules (e.g., "order can only be cancelled if not shipped")
- [ ] Application services are responsible for orchestration with other adapters and repositories
- [ ] Repository adapters validate data constraints, uniqueness, referential integrity (unique constraints, foreign-key existence, database-level constraints, data existence)
- [ ] All public methods and constructors for repositories, adapters, domains and services validate parameters with `ArgumentNullException.ThrowIfNull(param)` and `ArgumentException.ThrowIfNullOrWhiteSpace(param)` without redundant `nameof()`
- [ ] Application service methods wrap infrastructure exceptions with try-catch to add business context, but let domain exceptions (UnauthorizedAccessException, KeyNotFoundException) bubble up unchanged

#### Interface Placement (Critical for Clean Architecture)

For each Application interface ask: **Who consumes it?**

- ✅ **API layer** → KEEP in Application (prevents coupling, even if single implementation)
- ✅ **Multiple adapters** → KEEP in Application (shared contract)
- ⚠️ **Single adapter only** → Move to that adapter
- ❌ **Nobody (future use)** → Remove it

**Remove if present:**

- [ ] Configuration for unused features
- [ ] "For future other provider" abstractions (not built yet)

### Architecture Review Output

- List actual violations (with file:line references)
- Suggest removals for over-engineering
- For each interface: **WHO consumes it?** (API/adapters/none)
- Skip "could be improved" - only "must be fixed"

---

## B. OBSERVABILITY REVIEW

Review observability implementation for **[FEATURE_NAME]** feature:

### Observability Checklist

#### Tracing Tag Responsibility

- [ ] **Controllers** set `SetRequestTags` (incoming request data) and `SetResponseTags` (outgoing response data)
- [ ] **Application Services** set `SetExecutionTags` (business context, internal state) via `Activity.Current`

#### Implementation Feasibility

- [ ] Metrics can be implemented with current architecture (no circular dependencies)
- [ ] Logging doesn't require architectural changes or new interfaces
- [ ] No performance impact on critical paths

#### Signal Value

- [ ] Metrics provide signals NOT already available through headers/exceptions
- [ ] Logging helps identify actionable problems (attacks, misconfigurations, failures)
- [ ] No high-volume noise (exempt paths, successful requests, validation)

#### Lean Observability Principles

- [ ] Metrics answer: "What's broken?" or "Who's attacking?"
- [ ] Logging only for warnings/errors that require action OR low-volume informational events
- [ ] **LogInformation allowed for**:
  - Startup/shutdown events (adapter configuration, service initialization)
  - State transitions (circuit breaker opened/closed, cache invalidated)
  - Administrative operations (manual cache clear, config reload, triggered by admin)
  - Scheduled jobs (daily report started/completed, batch processing)
  - Lifecycle events (health status changed, migration applied)
- [ ] **LogInformation NOT allowed for**:
  - Per-request success operations (authentication succeeded, request processed)
  - Normal business operations (order created, email sent, file uploaded)
  - Validation failures (already throw clear exceptions)
- [ ] **Volume rule**: If it fires hundreds/thousands of times per minute → use LogWarning/LogError only for failures

### Observability Decision Framework

For each metric/log ask:

1. Can this actually be implemented without refactoring?
2. Is this information already in headers, exceptions, or existing metrics?
3. Does this signal an anomaly or just normal operation?
4. Will this add high-volume noise?
5. **For LogInformation**: What's the frequency/volume?
   - Once per deployment/hour/day → OK
   - Admin-triggered or scheduled → OK
   - Per request/transaction → NOT OK

**Rule**: If "already available" or "normal operation" → skip it.
**Volume Rule**: If LogInformation fires >100 times/minute → remove it or change to LogWarning for failures only.

### Observability Review Output

- List metrics to ADD (with justification)
- List metrics to REMOVE (with reason)
- List high-volume LogInformation to REMOVE (with reason)
- Confirm: LogInformation only for low-volume events (startup, admin actions, scheduled jobs, state changes)
- List logging to REMOVE (with reason)
- Confirm: No runtime success logs (no per-request LogInformation)
- Confirm: No trace/debug logs for successful operations

---

## C. DOCUMENTATION REVIEW

Review documentation for **[FEATURE_NAME]** feature:

### Documentation Checklist

#### Completeness

- [ ] Main README updated with feature overview
- [ ] Adapter README has complete usage guide
- [ ] /docs/ architecture files updated
- [ ] CHANGELOG.md entry added

#### Consistency (No Duplication)

- [ ] **Main README**: High-level overview + link to adapter
- [ ] **Adapter README**: Detailed implementation guide
- [ ] **/docs/**: Architecture deep-dives only
- [ ] No information repeated across multiple files

#### Quality

- [ ] Installation steps match current implementation
- [ ] Configuration examples match actual appsettings.json structure
- [ ] Code samples tested and working
- [ ] Metric names match actual implementation
- [ ] All cross-references/links are correct
- [ ] XML documentation comments are accurate and complete for all public APIs

#### Structure

- [ ] Follows project documentation patterns
- [ ] No stale references to removed features
- [ ] Limitations clearly stated
- [ ] Examples use consistent formatting

### Documentation Decision Framework

For each documentation file ask:

1. Is this the right place for this information?
2. Is this duplicated elsewhere?
3. Are examples current and tested?
4. Are limitations/constraints documented?

**Rule**: Each piece of information should exist in ONE authoritative location.

### Documentation Review Output

- List missing documentation
- List duplicated content to consolidate
- List outdated examples to update
- List broken cross-references to fix
- Confirm: CHANGELOG.md entry matches changes

---

## D. UNIT TESTING REVIEW

Review unit tests for **[FEATURE_NAME]** feature:

> **Note**: Validate line/branch coverage percentages via coverage report. This review focuses on test quality and best practices.

### Unit Testing Checklist

#### Test Quality & Maintainability

- [ ] Tests follow Arrange-Act-Assert (AAA) pattern
- [ ] Test names clearly describe what is being tested (Given_When_Then or MethodName_Scenario_ExpectedResult)
- [ ] Each test validates one specific behavior (no duplicate test logic)
- [ ] Tests are isolated (no dependencies on other tests)
- [ ] Tests are deterministic (no random values, no DateTime.Now)
- [ ] No tests checking implementation details (will break on safe refactoring)

#### Mocking Strategy

- [ ] Mocks used ONLY for external dependencies (database, HTTP, file system)
- [ ] Domain logic NOT mocked (test real implementations)
- [ ] Application services mocked when testing controllers
- [ ] Adapters mocked when testing application layer
- [ ] Mock setup is clear and minimal (no over-mocking)
- [ ] Verify interactions only when behavior matters (not implementation details)

#### Test Organization

- [ ] Tests in correct test project matching production structure
- [ ] Test classes mirror production code structure
- [ ] Integration tests separated from unit tests
- [ ] Test fixtures/helpers reused appropriately (no duplication)
- [ ] Each adapter has its own test project (e.g., `emc.camus.security.jwt.test`)

#### Assertions

- [ ] Assertions are specific (avoid Assert.True for complex conditions)
- [ ] Error messages provide context when test fails
- [ ] Multiple related assertions grouped logically
- [ ] No commented-out assertions
- [ ] Use FluentAssertions for readability where applicable
- [ ] **Exception message assertions**: Use wildcard patterns (e.g., `"*authentication*required*"`) not exact strings
- [ ] **Never assert on exception.Data**: Error codes are implementation details, assert on message patterns instead

### Unit Testing Decision Framework

For each test ask:

1. Does this test verify **behavior** or **implementation details**?
2. Will this test break when refactoring **without changing behavior**?
3. Is the test name clear enough to understand what failed?
4. Can this test run in isolation without setup from other tests?
5. Is the test in the correct layer's test project?
6. Is this test **duplicating** logic from another test?

**Rule**: If test checks implementation details, breaks on safe refactoring, or duplicates another test → rewrite or remove it.

### Unit Testing Review Output

- List tests that check **implementation details** (not behavior)
- List tests with **poor naming** (unclear what failed)
- List **duplicate tests** (same behavior tested multiple times)
- List tests with **unclear assertions** or over-mocking
- Confirm: Tests run successfully and independently
- Confirm: No integration tests in unit test projects
