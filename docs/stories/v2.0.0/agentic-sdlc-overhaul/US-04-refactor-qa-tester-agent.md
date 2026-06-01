# User Story Specification

## Metadata

- Story ID: `US-04`
- Owner: `3M0R4C`
- Status: `Todo`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the tester.qa agent to sign each story's Sections A–D
and contribute to the release-level QA section instead of moving stories to
done/`, so that `QA gating happens at the story level for completeness and at
the release level for test execution`.

### Functional Requirements

- FR-01: The `tester.qa` agent verifies that Sections A–D of a story are signed before approving it
- FR-02: The agent does not perform `git mv` to a `done/` folder (that folder no longer exists)
- FR-03: The agent updates the story's `Status` cell in its feature's `Stories` table to `QA` while reviewing and to
  `Done` after sign-off
- FR-04: The agent populates the `Local Validation` subsection of the release's `_release.md` QA section
- FR-05: The agent runs the full unit + integration suite and records pass/fail counts in the release's QA section
- FR-06: The agent does not sign the release-level `QA Handoff Gate` until every in-scope story is signed at the story
  level

### Non-Functional Requirements

- Security: `N/A`
- Performance: Full test suite execution time must not regress beyond current baseline (informational only)
- Observability: Agent reports each gate update with the story or release path
- Reliability: Agent must be re-runnable; signing a story already signed is a no-op
- Compliance: `concurrent.reviewer.copilot.customization` returns zero findings

### Acceptance Criteria

- AC-01: A repository-wide grep for `git mv` and `docs/stories/done/` returns zero matches in `tester.qa.agent.md`
- AC-02: The agent's flow explicitly references the release's `_release.md` QA section by path
- AC-03: The agent passes `concurrent.reviewer.copilot.customization`
- AC-04: The agent file passes `markdownlint-cli2`

### Notes

- Coordinates with US-05 (technical_writer) and US-06 (release_manager) on
  release-level gate ordering

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
