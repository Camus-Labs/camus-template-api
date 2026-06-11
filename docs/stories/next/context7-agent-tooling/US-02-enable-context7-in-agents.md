# User Story Specification

## Metadata

- Story ID: `US-02`
- Owner: `3M0R4C`
- Status: `In Progress`

## Section A - Product Owner Definition

### Story Statement

As a `repository contributor`, I want `the developer and tester.integration agents configured to
use the context7 MCP server with prescriptive usage guidelines`, so that `agents optimally leverage
context7 for library documentation lookups during implementation and integration testing tasks`.

### Functional Requirements

- FR-01: The `developer.agent.md` file MUST include `mcp: context7` in its `tools:` YAML
  frontmatter list.
- FR-02: The `tester.integration.agent.md` file MUST include `mcp: context7` in its `tools:` YAML
  frontmatter list.
- FR-03: The `developer.agent.md` body MUST contain prescriptive instructions requiring the agent
  to query context7 for library/framework documentation before implementing code that depends on
  external packages.
- FR-04: The `tester.integration.agent.md` body MUST contain prescriptive instructions requiring
  the agent to query context7 for library/framework documentation before writing integration tests
  that use third-party test infrastructure (e.g., Testcontainers, WebApplicationFactory patterns).
- FR-05: Only the `developer` and `tester.integration` agents MUST be modified; no other agent
  files MUST be changed.

### Non-Functional Requirements

- Security: N/A (agent configuration only, no runtime credentials involved).
- Performance: Context7 lookups MUST be targeted (specific library + version) to minimize token
  usage and response latency.
- Observability: N/A (tooling configuration, no runtime telemetry required).
- Reliability: Agent instructions MUST specify fallback behavior — if context7 is unreachable, the
  agent MUST proceed using its existing knowledge without failing the task.
- Compliance: N/A.

### Acceptance Criteria

- AC-01: Given the `developer.agent.md` file, when its YAML frontmatter is parsed, then
  `mcp: context7` is present in the `tools:` list.
- AC-02: Given the `tester.integration.agent.md` file, when its YAML frontmatter is parsed, then
  `mcp: context7` is present in the `tools:` list.
- AC-03: Given the developer agent is invoked for a task involving an external NuGet package, when
  the agent plans its implementation, then it MUST query context7 for that package's documentation
  before writing code.
- AC-04: Given the integration tester agent is invoked for a task involving Testcontainers or
  similar test infrastructure, when the agent plans its tests, then it MUST query context7 for the
  relevant library documentation before writing test code.
- AC-05: Given context7 is unavailable, when either agent attempts a lookup, then the agent
  proceeds with its task without error using built-in knowledge.

### Notes

- This story depends on US-01 (context7 server must be registered before agents can invoke it).
- The prescriptive guidelines should specify when to use `resolve-library-id` vs.
  `get-library-docs` context7 operations for optimal results.

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-06-10`

## Section B - Architect Definition

### Layer Impact Matrix

- Domain
  - Change summary: No change — story scope is agent configuration only.
  - Potential files/folders to touch: `N/A`
- Application
  - Change summary: No change — no new contracts, interfaces, or services required.
  - Potential files/folders to touch: `N/A`
- Database Schema
  - Change summary: No change — no migrations or schema modifications required.
  - Potential files/folders to touch: `N/A`
- API
  - Change summary: No change — no HTTP surface modifications.
  - Backward compatibility: `Backward compatible`
  - Potential files/folders to touch: `N/A`
- Adapters
  - Change summary: No change — no adapter implementations affected.
  - Potential files/folders to touch: `N/A`
- Tests
  - Change summary: No production test code required. Acceptance criteria are verified
    by inspecting the modified agent markdown files for correct YAML frontmatter and
    prescriptive body content. Validation is structural (file content assertions) and
    does not require xUnit test projects.
  - Potential files/folders to touch: `N/A`

### Delivery

Changes are scoped entirely to Copilot agent configuration files outside the
hexagonal architecture layers:

- `.github/agents/developer.agent.md` — add `mcp: context7` to `tools:` frontmatter
  list; add prescriptive instructions in body for querying context7 before implementing
  code that depends on external NuGet packages, specifying when to use
  `resolve-library-id` vs. `get-library-docs`; include fallback guidance when context7
  is unreachable.
- `.github/agents/tester.integration.agent.md` — add `mcp: context7` to `tools:`
  frontmatter list; add prescriptive instructions in body for querying context7 before
  writing integration tests that use third-party test infrastructure (Testcontainers,
  WebApplicationFactory patterns); include fallback guidance when context7 is
  unreachable.

No other agent files are modified per FR-05.

### Cross-Cutting Concern Decisions

Architectural decisions for satisfying the NFRs defined in Section A.

- Security: N/A — agent configuration only; no runtime credentials are introduced or
  stored. The context7 MCP server connection is established via the existing MCP
  registration from US-01.
- Performance: Agent instructions will mandate targeted queries specifying the exact
  library name and version to minimize token consumption and response latency. Agents
  must use `resolve-library-id` first to obtain the canonical identifier, then
  `get-library-docs` with a focused topic parameter.
- Observability: N/A — tooling configuration; no runtime telemetry changes.
- Reliability: Agent instructions will include an explicit fallback clause — if context7
  is unreachable or returns an error, the agent must proceed using its built-in
  knowledge without failing or blocking the task.
- Compliance: N/A — no regulatory or data-handling implications.

### Architect Handoff Gate

- Layer impacts are fully mapped: `Yes`
- Port | contract impacts assessed: `Yes`
- Backward compatibility decision documented: `Yes`
- Cross-cutting concern decisions addressed: `Yes`
- Ready for implementation: `Yes`
- Architect sign-off: `3M0R4C, 2026-06-10`

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

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `[Yes | No]`
- Skeleton inventory complete and user-approved: `[Yes | No]`
- Tests compile and fail for the right reason (TDD red): `[Yes | No]`
- Ready for developer implementation: `[Yes | No]`
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
