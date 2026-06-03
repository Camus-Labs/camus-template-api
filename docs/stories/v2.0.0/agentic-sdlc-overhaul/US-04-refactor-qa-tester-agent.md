# User Story Specification

## Metadata

- Story ID: `US-04`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the tester.qa agent to operate at the release level
and validate the assembled release once`, so that `QA gating happens after every
story is Done and produces a single signed QA Handoff Gate on _release.md`.

### Functional Requirements

- FR-01: The `tester.qa` agent accepts a path to `docs/stories/v<X.Y.Z>/_release.md` as input
- FR-02: The agent verifies that every in-scope story has `Metadata.Status: Done`, the feature `Stories` table
  row reads `Done`, and every Sections A–D handoff gate reads `Yes` or `N/A`
- FR-03: The agent runs the full unit + integration suite and records pass/fail counts in the release's QA section
- FR-04: The agent collects coverage and either closes gaps or records user acknowledgement in the release's QA
  section
- FR-05: The agent guides local validation (compose up, Postman, compose down) and records user confirmation in
  the release's QA section
- FR-06: The agent signs the release `QA Handoff Gate` only when every pre-flight check passes
- FR-07: The agent does not modify any `US-*.md` or `_feature.md` file (story `Done` status is set by the
  integration tester, or by the developer when Section D is `N/A`)
- FR-08: The agent does not perform `git mv` and does not reference `docs/stories/done/` or `docs/stories/todo/`
  (those folders no longer exist)

### Non-Functional Requirements

- Security: `N/A`
- Performance: Full test suite execution time must not regress beyond current baseline (informational only)
- Observability: Agent reports each pre-flight failure and each modified release-file path
- Reliability: Agent must be re-runnable; signing an already-signed release `QA Handoff Gate` is a no-op
- Compliance: `concurrent.reviewer.copilot.customization` returns zero findings

### Acceptance Criteria

- AC-01: A repository-wide grep for `git mv`, `docs/stories/done/`, and `docs/stories/todo/` returns zero matches
  in `tester.qa.agent.md` outside explicit rejection clauses
- AC-02: The agent's input is `release_file` (not `story_file`) and the flow explicitly references the release
  `QA Handoff Gate` by path
- AC-03: The agent passes `concurrent.reviewer.copilot.customization`
- AC-04: The agent file passes `markdownlint-cli2`

### Notes

- This story re-scopes the QA tester from story-level to release-level. The release flow is now:
  `developer → integration tester (marks story Done) → QA (release) → technical writer (release) → release manager`.
- US-05 (technical_writer) consumes the QA-validated release, so it runs after this agent signs the QA gate.

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-31`

## Section B - Architect Definition

N/A — prompt-only refactor of `tester.qa.agent.md`; no production code, layer, contract, or cross-cutting
concern is impacted.

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

Rewrote `.github/agents/tester.qa.agent.md` end-to-end as a release-level agent and updated companion agents so
story `Done` status is set by whoever signs the last applicable A–D gate:

- `tester.qa.agent.md` now takes a `release_file` input, enumerates every `US-*.md` under the release folder,
  verifies each story is `Done` with A–D gates `Yes`/`N/A`, runs the full suite, collects coverage, guides local
  validation, fills `_release.md` QA subsections, and signs the release `QA Handoff Gate`. It never modifies
  `US-*.md` or `_feature.md` files and never references `docs/stories/done/` or `docs/stories/todo/` outside
  explicit rejection clauses.
- `tester.integration.agent.md` Step 8 added: when the Integration Tester Handoff Gate is fully signed, the
  agent sets the story `Metadata.Status` to `Done` and updates the matching row in `<parent>/_feature.md`'s
  `Stories` table.
- `developer.agent.md` Step 11 added: when Section D is `N/A` and the Developer Handoff Gate is fully signed,
  the agent performs the same `Done` update; otherwise it leaves story `Status` unchanged for the integration
  tester to set.
- `docs/stories/_templates/_release.md` reordered so the `QA` section precedes the `Technical Writer` section,
  and the Technical Writer Handoff Gate now requires the QA Handoff Gate to be signed first.

New release flow: `developer → integration tester (marks Done) → QA (release) → technical writer (release) →
release manager`.

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
