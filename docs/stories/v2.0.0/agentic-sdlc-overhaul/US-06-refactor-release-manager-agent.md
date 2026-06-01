# User Story Specification

## Metadata

- Story ID: `US-06`
- Owner: `3M0R4C`
- Status: `Todo`

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

To be completed.

### Architect Handoff Readiness

- Ready for implementation: `No`
- Architect sign-off: `[pending]`

## Section C - Implementation Tracking

To be completed.

### Tester Handoff Gate

- Ready for implementation: `No`
- Tester sign-off: `[pending]`

### Developer Handoff Gate

- Ready for code review: `No`
- Developer sign-off: `[pending]`

## Section D - Integration Testing

To be completed.

### Integration Tester Handoff Gate

- Ready for review: `No`
- Integration Tester sign-off: `[pending]`
