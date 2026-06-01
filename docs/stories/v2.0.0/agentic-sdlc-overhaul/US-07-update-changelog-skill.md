# User Story Specification

## Metadata

- Story ID: `US-07`
- Owner: `3M0R4C`
- Status: `Todo`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the update-changelog skill to operate at the release
level`, so that `the technical writer agent can invoke it once per release and
produce a single consolidated changelog entry`.

### Functional Requirements

- FR-01: The `update-changelog` skill accepts a path to `docs/stories/v<X.Y.Z>/_release.md` as input
- FR-02: The skill consolidates per-story contributions from each feature's stories into a single `## [X.Y.Z] -
  YYYY-MM-DD` block in `CHANGELOG.md`
- FR-03: The skill groups changes by Keep-a-Changelog subsections (`Added`, `Changed`, `Deprecated`, `Removed`,
  `Fixed`, `Security`)
- FR-04: The skill marks any breaking changes with a `**BREAKING:**` prefix
- FR-05: The skill does not duplicate entries when re-run on the same release
- FR-06: The skill does not invent entries; it derives them from the stories' Section A statements and the release's
  `CHANGELOG Entry` subsection

### Non-Functional Requirements

- Security: `N/A`
- Performance: `N/A`
- Observability: Skill reports the release version, target file, and entry count
- Reliability: Skill must be idempotent
- Compliance: `markdownlint-cli2` passes on the resulting `CHANGELOG.md`

### Acceptance Criteria

- AC-01: Running the skill on `docs/stories/v2.0.0/_release.md` produces or updates a single `## [2.0.0]` block in
  `CHANGELOG.md`
- AC-02: Re-running the skill on the same release does not duplicate the block or its entries
- AC-03: The skill's SKILL.md passes `concurrent.reviewer.copilot.customization`
- AC-04: The skill's SKILL.md passes `markdownlint-cli2`

### Notes

- Sibling of US-05 (`technical_writer`); the agent invokes this skill

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
