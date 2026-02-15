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

**❌ FORBIDDEN (DON'T):**

- [ ] HTTP runtime objects (HttpContext, HttpRequest, HttpResponse)
- [ ] Infrastructure implementations (database, file I/O, caching, secrets)
- [ ] Configuration classes (Settings, Options)
- [ ] Interfaces for single-implementation application services (YAGNI - add when second implementation is needed)
- [ ] Middleware or DI registration
- [ ] Business/domain logic

##### Domain Layer (Business Logic)

**Purpose**: Pure business entities, rules, and logic. **No dependencies** on any other layer or infrastructure. This is where your business validation lives.

**✅ ALLOWED (DO):**

- [ ] Business entities and models (e.g., `User`, `Order`, `ApiInfo`, `Credentials`)
- [ ] Value objects (e.g., `Email`, `Money`, `Address`)
- [ ] Domain rules and business validation (e.g., "age >= 18", "price > 0", "email format valid")
- [ ] Domain exceptions for business rule violations (e.g., `InvalidAgeException`, `PriceCannotBeNegativeException`)
- [ ] Domain constants (business-related, e.g., `MinimumAge = 18`)
- [ ] Domain events (if using event-driven patterns)
- [ ] Extension methods for domain entities (e.g., `user.IsAdult()`, `order.CalculateTotal()`)

**❌ FORBIDDEN (DON'T):**

- [ ] Any infrastructure dependencies (database, HTTP, file system)
- [ ] Any Application layer references (attributes, exceptions)
- [ ] Framework-specific code (ASP.NET, EF Core annotations)

##### API Layer (HTTP Orchestration)

**Purpose**: HTTP pipeline, routing, and web infrastructure configuration. Orchestrates calls to Application interfaces.

**✅ ALLOWED (DO):**

- [ ] Controllers and endpoint handlers
- [ ] DTOs for HTTP translation (request/response models)
- [ ] Model binding attributes only (`[FromBody]`, `[FromQuery]`, `[FromRoute]`) - NO validation attributes
- [ ] Middleware (HTTP pipeline components)
- [ ] Action filters and exception filters (`IActionFilter`, `IExceptionFilter`)
- [ ] Dependency injection configuration (Program.cs, Startup.cs)
- [ ] HTTP pipeline configuration (middleware ordering, request processing)
- [ ] Error handling and response mapping
- [ ] Web infrastructure configuration (security policies, cross-cutting concerns)
- [ ] Service registration orchestration (calling adapter extension methods)
- [ ] Mapper extensions for DTO ↔ Command/Result conversion WITH validation

**❌ FORBIDDEN (DON'T):**

- [ ] Business/domain logic (calculations, rules, validation)
- [ ] Infrastructure implementations (database, secrets, caching)
- [ ] Using domain entities directly (map to DTOs in controllers)
- [ ] Validation attributes on DTOs (`[Required]`, `[StringLength]`, `[Range]`) - validation goes in mapper extensions

##### Adapters Layer (Infrastructure Implementations)

**Purpose**: Implements Application interfaces using specific technologies. Each adapter is independent and swappable.

**✅ ALLOWED (DO):**

- [ ] Implementation of Application interfaces
- [ ] Infrastructure-specific code (database, HTTP, file system, caching)
- [ ] External service clients and SDKs
- [ ] Adapter-specific interfaces (only consumed within adapter)
- [ ] Adapter-specific configuration classes (JwtSettings, DaprSecretProviderSettings, RateLimitSettings)
- [ ] Extension methods for service registration (AddJwtAuthentication, AddDaprSecrets)
- [ ] Adapter-specific middleware
- [ ] Technology-specific implementations (Dapr, Redis, JWT, etc.)

**❌ FORBIDDEN (DON'T):**

- [ ] Business/domain logic
- [ ] HTTP endpoint definitions
- [ ] Shared interfaces used by API (move to Application)

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
- [ ] Error messages are actionable and specific
- [ ] Code follows existing project patterns
- [ ] **All exceptions cascade to ExceptionHandlingMiddleware**
- [ ] Validation methods throw exceptions (never return null/false for validation failures)

#### Exception handling, Validation Pattern and Requests workflow

- [ ] All validation exceptions of any kind should go through ExceptionHandlingMiddleware for standard ProblemDetails output to be delivered on API calls.
- [ ] API Input validation happens in mapper extensions (API layer) for all DTOs fields, NOT in DTOs with DataAnnotations.
- [ ] API Input validates HTTP request format, required fields, data types, basic constraints.
- [ ] Controller layer uses Mapper extensions (API Layer) convert from/to API Request/Response DTOs to/from Application Commands/Results records to execute Application services.
- [ ] Application services used for interaction with API Layer are expected to validate business logic, workflow rules (e.g. "order can only be cancelled if not shipped").
- [ ] Application services are responsible for orchestration with other adapters and repositories.
- [ ] Domain entities use public constructors with validation on all attributes, to protect object integrity, enforce domain invariants, guarantee that entities cannot exist on invalid state.
- [ ] Domain entities Auto-generate ID when null allows flexibility for persistence layers and follow consistent patterns among them.
- [ ] Repository adapters should validate data constraints, uniqueness, referential integrity (Unique constraints, foreign-key existence, database-level constraints, data existence).
- [ ] Repository adapters use interfaces referencing domain objects.
- [ ] Validations should throw Exception messages that must be clear, actionable, and match test expectations.

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
