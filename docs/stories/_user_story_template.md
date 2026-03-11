# User Story Specification

## Metadata

- Story ID: `US-[###]`
- Feature Slug: `[feature-slug]`
- Story Slug: `[story-slug]`
- Request Date: `YYYY-MM-DD`
- Requested By: `[name or role]`

## Section A - Product Owner Definition

### Story Statement

As a `[persona]`, I want `[capability]`, so that `[outcome]`.

### Business Value

- [Value statement 1]
- [Value statement 2]

### In Scope

- [Scope item 1]
- [Scope item 2]

### Out of Scope

- [Out-of-scope item 1]
- [Out-of-scope item 2]

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

### Constraints and Dependencies

- Business constraints:
  - [Constraint 1]
  - [Constraint 2]
- Dependencies:
  - [Team | system | dependency 1]
  - [Team | system | dependency 2]

### Risks and Open Questions

- Risks:
  - [Risk 1 and mitigation owner]
  - [Risk 2 and mitigation owner]
- Open questions:
  - [Question 1]
  - [Question 2]

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `[Yes | No]`
- Story statement complete and outcome-focused: `[Yes | No]`
- Scope boundaries clear (in | out): `[Yes | No]`
- FRs atomic and testable: `[Yes | No]`
- NFRs specified across required categories: `[Yes | No]`
- Acceptance criteria measurable and complete: `[Yes | No]`
- Dependencies and constraints identified: `[Yes | No]`
- Risks and open questions documented: `[Yes | No]`
- Ready for architecture handoff: `[Yes | No]`
- Product Owner sign-off: `[Name, Date]`

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
