# User Story Specification

## Metadata

- Story ID: `US-[###]`
- Request Date: `YYYY-MM-DD`
- Requested By: `[name or role]`

## Section A - Product Owner Definition

### Story Statement

As a `[persona]`, I want `[capability]`, so that `[outcome]`.

### Functional Requirements

- FR-01: [Atomic functional requirement]
- FR-02: [Atomic functional requirement]
- FR-03: [Atomic functional requirement]

### Non-Functional Requirements

- Security: [AuthN | AuthZ, data protection, threat constraints]
- Performance: [Latency | throughput | scale target]
- Observability: [Logs, metrics, traces, alert hooks]
- Reliability: [Availability, retries, idempotency, fault tolerance]
- Compliance: [Policy, legal, audit, or regulatory requirement]

### Acceptance Criteria

- AC-01: [Verifiable behavior tied to one or more FRs]
- AC-02: [Verifiable behavior tied to one or more FRs]
- AC-03: [Verifiable behavior tied to one or more FRs]

### Notes

- [Optional: constraint, dependency, risk, or open question — only when non-obvious]

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `[Yes | No]`
- Story statement complete and outcome-focused: `[Yes | No]`
- FRs atomic and testable: `[Yes | No]`
- NFRs specified across required categories: `[Yes | No]`
- Acceptance criteria measurable and complete: `[Yes | No]`
- Ready for architecture handoff: `[Yes | No]`
- Product Owner sign-off: `[Name, Date]`

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

### Architect Handoff Readiness

- Layer impacts are fully mapped: `[Yes | No]`
- Port | contract impacts assessed: `[Yes | No]`
- Backward compatibility decision documented: `[Yes | No]`
- Cross-cutting concern decisions addressed: `[Yes | No]`
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

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
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
