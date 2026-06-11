# User Story Specification

## Metadata

- Story ID: `US-06`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the release_manager agent to drive the tag creation
and release/v<X.Y.Z> → main pull request flow at the release level`, so that
`promoting a validated release is a single deterministic operation tied to the
release's _release.md`.

### Functional Requirements

- FR-01: The `release_manager` agent accepts a path to `docs/stories/v<X.Y.Z>/_release.md` as input
- FR-02: The agent verifies that the `Technical Writer Handoff Gate` and `QA Handoff Gate` of the release are signed
- FR-03: The agent verifies that every feature in the release's `Features` table has status `Done`
- FR-04: The agent creates an annotated git tag `v<X.Y.Z>` on the head of `release/v<X.Y.Z>`
- FR-05: The agent opens a pull request from `release/v<X.Y.Z>` to `main` with a body summarizing the release (linking
  the `_release.md`)
- FR-06: The agent uses a rebase merge strategy for the release-to-main PR (no merge commits)
- FR-07: The agent signs the `Release Manager Handoff Gate` in the release file
- FR-08: The agent does not perform any per-story operations (no story-level moves or status changes)

### Non-Functional Requirements

- Security: Agent must rely on the user's existing `gh` authentication; no secrets are persisted by the agent
- Performance: `N/A`
- Observability: Agent reports the tag created, the PR URL, and the gate signed
- Reliability: Agent must abort with a clear error if any pre-flight check fails (gates not signed, features not Done,
  branch not pushed)
- Compliance: `concurrent.reviewer.copilot.customization` returns zero findings

### Acceptance Criteria

- AC-01: A repository-wide grep for per-story operations (e.g., `US-*.md`, `git mv`) in `release_manager.agent.md`
  returns zero matches
- AC-02: The agent's flow explicitly references `release/v<X.Y.Z>` and `main` branch names
- AC-03: The agent passes `concurrent.reviewer.copilot.customization`
- AC-04: The agent file passes `markdownlint-cli2`

### Notes

- Depends on US-04 (QA tester) and US-05 (technical writer) — both release gates
  must exist before this agent can verify them

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-31`

## Section B - Architect Definition

N/A — customization-only story (agent definition rewrite); no production code, schema, or HTTP-contract
change. No architectural design required.

### Layer Impact Matrix

`N/A` — non-runtime story; agent customization only. No runtime layers are affected.

### Cross-Cutting Concern Decisions

`N/A` — no NFRs require runtime architectural decisions.

### Architect Handoff Gate

- Layer impacts are fully mapped: `N/A`
- Port | contract impacts assessed: `N/A`
- Backward compatibility decision documented: `N/A`
- Cross-cutting concern decisions addressed: `N/A`
- Ready for implementation: `Yes`
- Architect sign-off: `3M0R4C, 2026-06-02`

## Section C - Implementation Tracking

Rewrote `.github/agents/release_manager.agent.md` to operate at release scope: accepts `release_file`,
verifies the Technical Writer and QA gates, verifies every feature in `Features` table is `Done`, creates an
annotated `v<X.Y.Z>` tag on `release/v<X.Y.Z>`, opens the `release/v<X.Y.Z> → main` PR with rebase merge
strategy, sets `_release.md` `Metadata.Status` to `Released`, and signs the `Release Manager Handoff Gate`.
Removed all per-story operations (no story-level commits, no `git mv`, no `US-*.md` mutations).

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `.github/agents/` only.

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `N/A`
- Skeleton inventory complete and user-approved: `N/A`
- Tests compile and fail for the right reason (TDD red): `N/A`
- Ready for developer implementation: `Yes`
- Unit Tester sign-off: `3M0R4C, 2026-06-02`

### Regression Fixes Log

| # | Test File | Test Method | Change Made | Reason |
| --- | --- | --- | --- | --- |
| — | — | — | None | No code changes |

### Developer Handoff Gate

- All unit tests pass (TDD green): `N/A`
- All existing integration tests pass: `N/A`
- Regression fixes documented (if any): `N/A`
- Build succeeds with zero warnings: `N/A`
- Ready for code review: `Yes`
- Developer sign-off: `3M0R4C, 2026-06-02`

## Section D - Integration Testing

N/A — customization-only story; no integration tests applicable.

### Integration Test Traceability

`N/A` — non-runtime story; no cross-layer boundaries to exercise.

### Integration Test Findings

`N/A` — no integration tests run.

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `N/A`
- All integration tests pass: `N/A`
- No unresolved production code findings: `N/A`
- Ready for review: `Yes`
- Integration Tester sign-off: `3M0R4C, 2026-06-02`
