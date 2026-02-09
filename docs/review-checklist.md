# Pre-Commit Feature Review Checklist

This checklist ensures new features follow architectural principles, avoid over-engineering, and maintain documentation consistency.

**Usage**: Replace `[FEATURE_NAME]` with the actual feature (e.g., "rate limiting", "JWT authentication"). Execute sections in order: Architecture → Observability → Documentation.

---

## A. ARCHITECTURE REVIEW

Review the **[FEATURE_NAME]** feature implementation:

### Architecture Checklist

#### Clean/Hexagonal Compliance

- [ ] Application layer does NOT depend on infrastructure (violation if true)
- [ ] Abstractions (interfaces/exceptions) in Application are used by API or multiple adapters
- [ ] Adapter-specific interfaces are kept in adapters (correct placement)
- [ ] Dependency direction follows: API/Adapters → Application → Domain (never reverse)

#### Layer Placement Validation

- [ ] **Application**: Only shared contracts (attributes, exceptions, constants)
- [ ] **Adapters**: Implementation details, infrastructure, HTTP/DB specifics
- [ ] **Domain**: Business entities and rules
- [ ] **API**: Orchestration and HTTP concerns

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

#### Over-Engineering Check (YAGNI)

**❌ Remove if present:**

- [ ] Interfaces with single implementation (keep in adapter if only one)
- [ ] Abstractions "for future Redis/other adapters" (not building now)
- [ ] Configuration for unused features
- [ ] Metrics/logging for normal operations (not anomalies)
- [ ] "Extensibility points" with no current use case

**✅ Keep only if:**

- Used by multiple implementations RIGHT NOW
- Fixes actual current problem
- Required by framework/architecture rules

#### Observability Inclusion (Lean Approach)

- [ ] **Metrics**: Only anomalies/errors (rejections, failures, misconfigurations)
- [ ] **Logging**: Only actionable warnings (attacks, config errors)
- [ ] **Skip**: Success cases, validation (throws clear exceptions), high-volume normal operations

### DECISION FRAMEWORK

For each component ask:

1. What current problem does this solve? (not future)
2. What breaks if removed today?
3. Is this adding complexity or reducing it?
4. Is this information already available? (headers, exceptions, existing metrics)

**Rule**: If "nothing breaks" and "adds complexity" → remove it.

### Architecture Review Output

- List actual violations found (with file:line references)
- Suggest removals for over-engineering
- Propose moves ONLY if violating dependency rules
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
- [ ] Logging only for warnings/errors that require action
- [ ] Success cases are NOT logged or metered
- [ ] Validation failures already throw clear exceptions (no logging needed)

### Observability Decision Framework

For each metric/log ask:

1. Can this actually be implemented without refactoring?
2. Is this information already in headers, exceptions, or existing metrics?
3. Does this signal an anomaly or just normal operation?
4. Will this add high-volume noise?

**Rule**: If "already available" or "normal operation" → skip it.

### Observability Review Output

- List metrics to ADD (with justification)
- List metrics to REMOVE (with reason)
- List logging to ADD (with level and condition)
- List logging to REMOVE (with reason)
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

### Unit Testing Checklist

#### Test Coverage

- [ ] All public methods have corresponding tests
- [ ] Happy path scenarios covered
- [ ] Error/exception paths tested
- [ ] Edge cases and boundary conditions covered
- [ ] No untested business logic

#### Test Quality

- [ ] Tests follow Arrange-Act-Assert (AAA) pattern
- [ ] Test names clearly describe what is being tested (Given_When_Then or MethodName_Scenario_ExpectedResult)
- [ ] Each test validates one specific behavior
- [ ] Tests are isolated (no dependencies on other tests)
- [ ] Tests are deterministic (no random values, no DateTime.Now)

#### Mocking Strategy

- [ ] Mocks used only for external dependencies (database, HTTP, file system)
- [ ] Domain logic NOT mocked (test real implementations)
- [ ] Application services mocked when testing controllers
- [ ] Adapters mocked when testing application layer
- [ ] Mock setup is clear and minimal
- [ ] Verify interactions only when behavior matters (not implementation details)

#### Test Organization

- [ ] Tests in correct test project matching production structure
- [ ] Test classes mirror production code structure
- [ ] Integration tests separated from unit tests
- [ ] Test fixtures/helpers reused appropriately
- [ ] Each adapter has its own test project (e.g., `emc.camus.security.jwt.test`)

#### Assertions

- [ ] Assertions are specific (avoid Assert.True for complex conditions)
- [ ] Error messages provide context when test fails
- [ ] Multiple related assertions grouped logically
- [ ] No commented-out assertions
- [ ] Use FluentAssertions for readability where applicable

### Unit Testing Decision Framework

For each test ask:

1. Does this test verify behavior or implementation details?
2. Will this test break when refactoring without changing behavior?
3. Is the test name clear enough to understand what failed?
4. Can this test run in isolation without setup from other tests?
5. Is the test in the correct layer's test project?

**Rule**: If test checks implementation details or breaks on safe refactoring → rewrite it.

### Unit Testing Review Output

- List untested public methods/classes
- List tests that need improvement (poor naming, unclear assertions, testing implementation)
- List missing test scenarios (edge cases, error paths)
- Confirm: Tests run successfully and independently
- Confirm: No integration tests in unit test projects
