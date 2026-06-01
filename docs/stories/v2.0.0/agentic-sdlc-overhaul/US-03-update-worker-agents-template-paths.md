# User Story Specification

## Metadata

- Story ID: `US-03`
- Owner: `3M0R4C`
- Status: `Todo`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the architect, developer, tester.unit, and
tester.integration agents to reference the new template location`, so that
`worker agents continue to operate against the canonical user story template
after the layout migration`.

### Functional Requirements

- FR-01: `architect.agent.md` references `docs/stories/_templates/_user_story.md` (not the legacy
  `docs/stories/_user_story_template.md`)
- FR-02: `developer.agent.md` references `docs/stories/_templates/_user_story.md`
- FR-03: `tester.unit.agent.md` references `docs/stories/_templates/_user_story.md`
- FR-04: `tester.integration.agent.md` references `docs/stories/_templates/_user_story.md`
- FR-05: All four agents accept story paths under `docs/stories/v<X.Y.Z>/<feature-slug>/US-*.md` as valid input
- FR-06: All four agents reject story paths under `docs/stories/done/` or `docs/stories/todo/`
- FR-07: Each agent's gate references match the new four-section structure (no references to Section E or F)

### Non-Functional Requirements

- Security: `N/A`
- Performance: `N/A`
- Observability: `N/A`
- Reliability: Agents must remain backward-compatible with the existing migrated stories under `v1.0.0/` and `v1.0.1/`
- Compliance: `concurrent.reviewer.copilot.customization` returns zero findings on each updated agent

### Acceptance Criteria

- AC-01: A repository-wide grep for `docs/stories/_user_story_template.md` and `docs/stories/done/` returns zero
  matches inside `.github/agents/`
- AC-02: Each of the four agents passes `concurrent.reviewer.copilot.customization`
- AC-03: Each updated agent file passes `markdownlint-cli2`

### Notes

- This is intentionally a small path-only update; behavioral changes to these
  agents are out of scope for this story

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
