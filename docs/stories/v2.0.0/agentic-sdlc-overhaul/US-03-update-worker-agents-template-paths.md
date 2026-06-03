# User Story Specification

## Metadata

- Story ID: `US-03`
- Owner: `3M0R4C`
- Status: `Done`

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

N/A — path-only documentation update to four agent files; no production code, layer, contract, or
cross-cutting concern is impacted.

### Layer Impact Matrix

`N/A` — non-runtime story; agent customization only. No runtime layers are affected.

### Cross-Cutting Concern Decisions

`N/A` — no NFRs require runtime architectural decisions.

### Architect Handoff Readiness

- Layer impacts are fully mapped: `N/A`
- Port | contract impacts assessed: `N/A`
- Backward compatibility decision documented: `N/A`
- Cross-cutting concern decisions addressed: `N/A`
- Ready for implementation: `Yes`
- Architect sign-off: `N/A, 2026-06-02`

## Section C - Implementation Tracking

Updated four worker agent files to reference the new template location and explicitly reject legacy story paths:

- `architect.agent.md` — Context updated to `docs/stories/_templates/_user_story.md`; `story_file` input restricted to
  `docs/stories/v<X.Y.Z>/<feature-slug>/US-*.md`.
- `developer.agent.md` — same updates as above.
- `tester.unit.agent.md` — same updates as above.
- `tester.integration.agent.md` — same updates as above, plus Step 7 prose updated to reference the new template path.

All four agents already used the four-section structure (no Section E or F references existed); no gate text changes
were required. Verified via repository grep that `_user_story_template.md` no longer appears in any of the four files
and that the only remaining `done/`/`todo/` references are the new explicit rejection clauses per FR-06.

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `.github/agents/` only.

### Tester Handoff Gate

- Every acceptance criterion has at least one test method: `N/A`
- Skeleton inventory complete and user-approved: `N/A`
- Tests compile and fail for the right reason (TDD red): `N/A`
- Ready for implementation: `Yes`
- Tester sign-off: `N/A, 2026-06-02`

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

N/A — no runtime code paths are affected by this documentation update.

### Integration Test Traceability

`N/A` — non-runtime story; no cross-layer boundaries to exercise.

### Integration Test Findings

`N/A` — no integration tests run.

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `N/A`
- All integration tests pass: `N/A`
- No unresolved production code findings: `N/A`
- Ready for review: `Yes`
- Integration Tester sign-off: `N/A, 2026-06-02`
