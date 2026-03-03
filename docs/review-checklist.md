# Pre-Commit Feature Review Checklist

Execute sections in order for `[FEATURE_NAME]`: Architecture → Observability → Documentation → Unit Testing.

---

## A. ARCHITECTURE REVIEW

Find and review all classes, enums, interfaces, and files related to `[FEATURE_NAME]`.

### Clean/Hexagonal Compliance

- [ ] Application layer does NOT depend on infrastructure
- [ ] Abstractions (interfaces/exceptions) in Application are consumed by API or multiple adapters
- [ ] Adapter-specific interfaces are kept inside their adapter
- [ ] Dependency direction: API/Adapters → Application → Domain (never reverse)

### Application Layer

Purpose: Contracts (interfaces, attributes, exceptions) that decouple API from adapters. NOT for business logic.

**DO:**

- [ ] Custom attributes for cross-cutting concerns (e.g., `[RateLimit]`)
- [ ] Custom exceptions for infrastructure failures (e.g., `RateLimitExceededException`)
- [ ] Port interfaces for adapters with multiple implementations (e.g., `ITokenGenerator`, `IUserRepository`)
- [ ] Interfaces consumed by API layer or multiple adapters
- [ ] Constants for cross-cutting concerns (e.g., `ErrorCodes`, `Headers`)
- [ ] Application services as concrete classes — no interface unless multiple implementations exist
- [ ] CQRS-style type files per feature folder:
  - `*Commands.cs` — positional records for write inputs
  - `*Results.cs` — positional records for write outputs, only when no View matches the shape
  - `*Filters.cs` — positional records with defaults for query inputs
  - `*Views.cs` — positional records for query output projections
- [ ] Views as default return type for both queries and commands — create a Result only when the output has a
  genuinely different shape (e.g., `GenerateTokenResult` includes raw JWT, which no View exposes)
- [ ] View naming describes content/shape, not query method — `GeneratedTokenSummaryView` (correct) vs
  `GeneratedTokenByUserView` (wrong)
- [ ] Filter naming targets the entity, not a view — `GeneratedTokenFilter` (correct) vs `GeneratedTokenSummaryFilter`
  (wrong)
- [ ] Common types in `Common/` folder: `PaginationParams` (constructor-validated, `get`-only), `PagedResult<T>`
  (generic paginated container)
- [ ] Constructor-based validation for value objects — constructor enforces invariants, no `init` setters that
  bypass validation
- [ ] Private mapping helpers in services (e.g., `ToSummaryView`) for entity → view conversion

**DON'T:**

- [ ] HTTP runtime objects (`HttpContext`, `HttpRequest`, `HttpResponse`)
- [ ] Infrastructure implementations (database, file I/O, caching, secrets)
- [ ] Configuration classes (`Settings`, `Options`)
- [ ] Interfaces for single-implementation application services
- [ ] Middleware or DI registration
- [ ] Business/domain logic
- [ ] Factory methods with `init` setters on value objects
- [ ] Unbounded list queries for growing datasets without pagination — acceptable for naturally bounded datasets
  (e.g., API versions, enum-like reference data)

### Domain Layer

Purpose: Pure business entities, rules, and logic. No dependencies on any other layer.

**DO:**

- [ ] Business entities with private setters on all properties
- [ ] Value objects (e.g., `Email`, `Money`)
- [ ] Domain rules and business validation
- [ ] Domain exceptions for business rule violations
- [ ] Domain constants
- [ ] Domain events (if using event-driven patterns)
- [ ] Extension methods for domain entities
- [ ] Public constructor for new entities — validates business attributes, sets initial state, auto-generates ID
  when null
- [ ] `Reconstitute` static factory for rebuilding from persistence — accepts all fields, skips business validation
- [ ] Business methods for state transitions (e.g., `Revoke()`) — enforce invariants, the only way to mutate state
  after construction
- [ ] Lifecycle/audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) are read-only — populated only via
  `Reconstitute`

**DON'T:**

- [ ] Infrastructure dependencies (database, HTTP, file system)
- [ ] Application layer references or framework-specific code
- [ ] `DateTime.UtcNow` or clock dependencies — timestamps are infrastructure concerns
- [ ] Constructors or business methods setting lifecycle/audit fields

### API Layer

Purpose: HTTP pipeline, routing, and web infrastructure. Orchestrates calls to Application.

**DO:**

- [ ] Controllers and endpoint handlers
- [ ] DTOs separate from Application views — versioning-ready contract boundary
- [ ] DTO folder conventions: `Models/Dtos/` for item DTOs, `Models/Responses/` for envelopes, `Models/Requests/` for
  input models
- [ ] Versioned types in version folders where they originate (e.g., `Models/Dtos/V1/`, `Models/Requests/V2/`)
- [ ] Version folder created only when a type's shape diverges — if shapes are identical, reuse the original version's
  type
- [ ] Shared infrastructure types unversioned in parent folder (e.g., `PaginationQuery`, `ApiResponse<T>`,
  `PagedResponse<T>`)
- [ ] Version independence — V2 types never inherit from or reference V1 types
- [ ] Model binding attributes only (`[FromBody]`, `[FromQuery]`, `[FromRoute]`)
- [ ] Middleware and HTTP pipeline configuration
- [ ] Action filters and exception filters
- [ ] DI and service registration in `Program.cs`
- [ ] `Infrastructure/` folder for framework-dependent service implementations (e.g., `HttpUserContext` implementing
  `IUserContext`) — distinct from `Models/` (data shapes) and `Mapping/` (converters)
- [ ] Feature-specific mappers in version folders (e.g., `Mapping/V1/ApiInfoMappingExtensions`,
  `Mapping/V2/AuthMappingExtensions`)
- [ ] Reusable mappers unversioned (`Mapping/CommonMappingExtensions` — `ToPaginationParams()`, `ToPagedResponse()`)
- [ ] `[ProducesResponseType]` for success responses only (200, 201, 204) with typed payloads

**DON'T:**

- [ ] Business/domain logic
- [ ] Infrastructure implementations (database, secrets, caching)
- [ ] Domain entities in controllers — map to DTOs
- [ ] Application views returned directly — DTOs are the API contract
- [ ] Validation attributes on DTOs (`[Required]`, `[StringLength]`, `[Range]`) — validation in mapper extensions
- [ ] `[ProducesResponseType]` for error responses — `DefaultApiResponsesOperationFilter` adds these globally

### Adapters Layer

Purpose: Implements Application interfaces using specific technologies. Each adapter is independent and swappable.

**DO:**

- [ ] Implementation of Application interfaces
- [ ] Technology-specific code (database, caching, Dapr, Redis, JWT, etc.)
- [ ] External service clients and SDKs
- [ ] Adapter-specific interfaces (consumed only within adapter)
- [ ] Adapter-specific configuration classes (e.g., `JwtSettings`, `RateLimitSettings`)
- [ ] Extension methods for service registration (e.g., `AddJwtAuthentication()`)
- [ ] Adapter-specific middleware

**DON'T:**

- [ ] Business/domain logic
- [ ] HTTP endpoint definitions
- [ ] Interfaces consumed by API — move to Application

### Repository Adapter Conventions

Purpose: Database persistence with specific data mapping, lifecycle, and write patterns.

**DO:**

- [ ] Database models in `Models/` with `*Model` suffix (e.g., `UserModel`) — Dapper maps rows to these
- [ ] Mapping extensions in `Mapping/` with `*MappingExtensions` suffix — `ToEntity()` via `Entity.Reconstitute()`
- [ ] Each Model has a corresponding MappingExtensions class
- [ ] Lifecycle fields owned by repository: `created_at` via `DEFAULT NOW()`, `updated_at` on UPDATE,
  `created_by`/`updated_by` from `IUserContext`
- [ ] Entity-centric writes for entities with state transitions — service constructs/mutates entity, repository
  persists (load → mutate → save)
- [ ] Parameter-based writes for infrastructure operations without domain behavior (e.g., `AuthenticateAsync`
  with BCrypt)
- [ ] `Create` accepts domain entity, extracts fields for INSERT, owns lifecycle defaults
- [ ] `Update/Save` accepts domain entity, persists mutated state, owns lifecycle updates
- [ ] Read methods return domain entities — service maps to Views
- [ ] Paginated queries with `PagedResult<T>`, `PaginationParams`, and SQL `LIMIT/OFFSET` — server-side only
- [ ] Filters passed from service for dynamic `WHERE` clause construction with `DynamicParameters`

**DON'T:**

- [ ] Inline/nested DTO classes — use `Models/` folder
- [ ] Inline Model-to-Entity mapping — use `Mapping/` folder
- [ ] Business rules duplicated in SQL
- [ ] Infrastructure internals exposed to domain or application (e.g., password hashes)
- [ ] Unbounded list queries for growing datasets — acceptable for naturally bounded datasets (e.g., API versions,
  roles)

### Configuration Classes

Settings classes in adapters follow this pattern:

- [ ] Enums for type-safe options — exception: validated strings for framework-mandated identifiers
- [ ] `Enum.IsDefined()` validation to catch misconfigurations
- [ ] Validation logic as private `ValidateXxx()` methods called from `Validate()`
- [ ] Each property has its own validation method
- [ ] Validation constants as `private const` fields
- [ ] XML exception documentation on `Validate()` method
- [ ] No separate validator classes — validation lives with the data

### Cross-Cutting Standards

- [ ] All validation exceptions cascade to `ExceptionHandlingMiddleware` for `ProblemDetails` output
- [ ] Validation methods throw exceptions — never return null/false
- [ ] Exception messages are clear, actionable, and match test expectations
- [ ] All public methods/constructors validate parameters with `ArgumentNullException.ThrowIfNull()` and
  `ArgumentException.ThrowIfNullOrWhiteSpace()` — no redundant `nameof()`
- [ ] Service methods wrap infrastructure exceptions with try-catch for business context — domain exceptions bubble up
  unchanged
- [ ] No magic numbers/strings — use constants
- [ ] No duplicate code/logic across files
- [ ] XML documentation on all public APIs

### Validation Flow by Layer

- [ ] API: Validates HTTP format, required fields, basic constraints
- [ ] Mapper extensions: Convert DTOs → Commands/Filters with input validation
- [ ] Application services: Validate business logic and orchestrate
- [ ] Repository adapters: Validate data constraints, uniqueness, referential integrity

### Interface Placement

For each Application interface, verify consumer:

- [ ] Consumed by API layer → keep in Application
- [ ] Consumed by multiple adapters → keep in Application
- [ ] Consumed by single adapter only → move to that adapter
- [ ] Consumed by nobody → remove
- [ ] No configuration for unused features
- [ ] No future-use abstractions not yet needed

### Review Output

- List violations with file:line references — only "must be fixed", not "could be improved"
- For each interface: who consumes it (API/adapters/none)
- Suggest removals for over-engineering

---

## B. OBSERVABILITY REVIEW

Review observability for `[FEATURE_NAME]`.

### Tracing

- [ ] Controllers set `SetRequestTags` (request data) and `SetResponseTags` (response data)
- [ ] Application services set `SetExecutionTags` (business context) via `Activity.Current`

### Metrics

- [ ] Implementable with current architecture (no circular dependencies)
- [ ] Provide signals not already in headers/exceptions
- [ ] Answer: "What's broken?" or "Who's attacking?"

### Logging

- [ ] No architectural changes required
- [ ] No performance impact on critical paths
- [ ] Identifies actionable problems (attacks, misconfigurations, failures)
- [ ] No high-volume noise (exempt paths, successful requests)

### LogInformation Rules

**ALLOWED:**

- [ ] Startup/shutdown events (adapter configuration, service initialization)
- [ ] State transitions (circuit breaker opened/closed, cache invalidated)
- [ ] Administrative operations (manual cache clear, config reload)
- [ ] Scheduled jobs (batch processing started/completed)
- [ ] Lifecycle events (health status changed, migration applied)

**FORBIDDEN:**

- [ ] Per-request success operations (authentication succeeded, request processed)
- [ ] Normal business operations (order created, email sent)
- [ ] Validation failures (already throw clear exceptions)
- [ ] Volume > 100/minute — use LogWarning/LogError for failures only

### Review Output

- Metrics to add/remove with justification
- High-volume LogInformation to remove
- Confirm: no per-request LogInformation, no trace/debug logs for success operations

---

## C. DOCUMENTATION REVIEW

Review documentation for `[FEATURE_NAME]`.

### Completeness

- [ ] Main README updated with feature overview
- [ ] Adapter README has complete usage guide
- [ ] `/docs/` architecture files updated
- [ ] CHANGELOG.md entry added

### Single Source of Truth

- [ ] Main README: high-level overview + link to adapter
- [ ] Adapter README: detailed implementation guide
- [ ] `/docs/`: architecture deep-dives only
- [ ] No information duplicated across files

### Accuracy

- [ ] Installation steps match current implementation
- [ ] Configuration examples match actual `appsettings.json`
- [ ] Code samples tested and working
- [ ] Metric names match implementation
- [ ] Cross-references/links are valid
- [ ] XML documentation comments are complete for all public APIs

### Structure

- [ ] Follows project documentation patterns
- [ ] No stale references to removed features
- [ ] Limitations clearly stated
- [ ] Consistent formatting

### Review Output

- Missing documentation, duplicated content, outdated examples, broken links
- Confirm CHANGELOG.md matches changes

---

## D. UNIT TESTING REVIEW

Review tests for `[FEATURE_NAME]`. Coverage percentages validated via coverage report.

### Quality

- [ ] Arrange-Act-Assert (AAA) pattern
- [ ] Test names: `MethodName_Scenario_ExpectedResult` or `Given_When_Then`
- [ ] Each test validates one specific behavior
- [ ] Tests are isolated and deterministic (no random values, no `DateTime.Now`)
- [ ] No tests checking implementation details

### Mocking

- [ ] Mocks only for external dependencies (database, HTTP, file system)
- [ ] Domain logic NOT mocked — test real implementations
- [ ] Application services mocked when testing controllers
- [ ] Adapters mocked when testing application layer
- [ ] Mock setup minimal — verify interactions only when behavior matters

### Organization

- [ ] Tests in correct project matching production structure
- [ ] Test classes mirror production code structure
- [ ] Integration tests separated from unit tests
- [ ] Test fixtures/helpers reused (no duplication)
- [ ] Each adapter has its own test project

### Assertions

- [ ] Specific assertions (no `Assert.True` for complex conditions)
- [ ] Exception messages: wildcard patterns (e.g., `"*authentication*required*"`) — not exact strings
- [ ] Never assert on `exception.Data` — assert on message patterns instead
- [ ] No commented-out assertions

### Review Output

- Tests checking implementation details (not behavior)
- Tests with poor naming or unclear assertions
- Duplicate tests (same behavior tested multiple times)
- Confirm: tests run independently, no integration tests in unit test projects
