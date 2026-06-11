# User Story Specification

## Metadata

- Story ID: `US-07`
- Owner: `3M0R4C`
- Status: `Done`

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

- AC-01: Running the skill on `docs/stories/next/_release.md` produces or updates a single `## [X.Y.Z]` block in
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

N/A — skill-only refactor; no production code, layer, contract, or cross-cutting concern is impacted.

### Layer Impact Matrix

`N/A` — non-runtime story; skill customization only. No runtime layers are affected.

### Cross-Cutting Concern Decisions

`N/A` — no NFRs require runtime architectural decisions.

### Architect Handoff Gate

- Layer impacts are fully mapped: `N/A`
- Port | contract impacts assessed: `N/A`
- Backward compatibility decision documented: `N/A`
- Cross-cutting concern decisions addressed: `N/A`
- Ready for implementation: `Yes`
- Architect sign-off: `N/A, 2026-06-02`

## Section C - Implementation Tracking

Rewrote `.github/skills/update-changelog/SKILL.md` to operate on a release file instead of a single story:

- Argument is `release_file` pointing to `_release.md` (rejects any other filename).
- Enumerates every `US-*.md` in the release folder and summarizes Functional Requirements from Section A of
  each story into grouped, user-facing imperative entries (`Added`, `Changed`, `Fixed`, `Removed`, `Security`,
  `Deprecated`).
- Computes the next semver from `_release.md` `Release Type` against `Directory.Build.props` current version,
  or honors `Release Version` when it is not a placeholder.
- Confirms the version with the user, then writes a single `## [vX.Y.Z] - <today>` section to `CHANGELOG.md`
  containing every grouped entry and updates `Directory.Build.props` `<Version>`.
- SUCCESS payload now reports `version`, `previous_version`, `bump_type`, `date`, `stories_summarized`,
  `entries_added`, and `changelog_lines` so `technical_writer` can paste them into `_release.md` directly.

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `.github/skills/` only.

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `N/A`
- Skeleton inventory complete and user-approved: `N/A`
- Tests compile and fail for the right reason (TDD red): `N/A`
- Ready for developer implementation: `Yes`
- Unit Tester sign-off: `N/A, 2026-06-02`

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

N/A — no runtime code paths are affected by this skill update.

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
