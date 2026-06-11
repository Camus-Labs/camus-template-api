# User Story Specification

## Metadata

- Story ID: `US-09`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `release captain operating the agentic SDLC`, I want `the concurrent reviewer agents to optionally apply
and commit the fixes they reported, and an additional release-level code review phase before QA`, so that
`I can close review findings without leaving chat and catch cross-story regressions introduced by squash
merges before spending QA cycles`.

### Functional Requirements

- FR-01: `concurrent.reviewer.code` delivers its report first and, on `FAIL` verdict, asks the user whether to
  apply the reported fixes in the current session
- FR-02: `concurrent.reviewer.documentation` delivers its report first and, on `FAIL` verdict, asks the user
  whether to apply the reported fixes in the current session
- FR-03: `concurrent.reviewer.copilot.customization` delivers its report first and, on `FAIL` verdict, asks the
  user whether to apply the reported fixes in the current session
- FR-04: Each reviewer, after applying fixes, shows `git status --short` and asks the user to confirm before
  staging, committing, and pushing the fixes to the current branch
- FR-05: Each reviewer instructs the user to re-validate by starting a new chat session and re-invoking the
  same reviewer with the same scope (never re-runs the review in the same session)
- FR-06: `docs/agentic-sdlc-workflow.md` declares a `Phase 6: Release Code Review` invoking
  `@concurrent.reviewer.code` against the release branch (`release/v<X.Y.Z>` diff vs `main`), placed between
  `/complete-feature` and QA, with QA, Technical Writer, Documentation Review, and Release Manager renumbered
  to Phases 7, 8, 9, and 10
- FR-07: The standalone `code.fix.agent` and `documentation.fix.agent` files are removed (their behavior is
  superseded by the reviewer agents' post-report fix flow)

### Non-Functional Requirements

- Security: Reviewers must require an explicit user `yes` before any `git add`, `git commit`, or `git push`
  invocation
- Performance: `N/A`
- Observability: `N/A`
- Reliability: Reviewers must not re-run the review after applying fixes (re-validation belongs in a new
  session to avoid confirmation bias)
- Compliance: All modified markdown files pass `markdownlint-cli2` with zero errors

### Acceptance Criteria

- AC-01: `.github/agents/concurrent.reviewer.code.agent.md` Process section contains a post-report FAIL prompt,
  a fix step, a `git status --short` confirmation prompt, a commit + push step, and a new-session
  re-validation instruction
- AC-02: `.github/agents/concurrent.reviewer.documentation.agent.md` Process section contains the same five
  elements as AC-01, scoped to documentation files
- AC-03: `.github/agents/concurrent.reviewer.copilot.customization.agent.md` Process section contains the same
  five elements as AC-01, scoped to the target customization file
- AC-04: `docs/agentic-sdlc-workflow.md` ASCII diagram, Mermaid diagram, Phases in Detail prose, Quick
  Reference table, and Tips list all reflect Phase 6 Release Code Review and the renumbered Phases 7–10
- AC-05: `.github/agents/code.fix.agent.md` and `.github/agents/documentation.fix.agent.md` no longer exist in
  the repository
- AC-06: `markdownlint-cli2` passes on every modified file with zero errors

### Notes

- The release-level code review uses the existing `concurrent.reviewer.code` agent — no new reviewer agent is
  introduced; only the workflow declares a second invocation point with `release/v<X.Y.Z>` scope.
- Per-story Phase 5 reviews still run on `feat/<slug>` before `/complete-feature`; Phase 6 is additive and
  catches cross-story regressions invisible to per-story reviews.

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-06-02`

## Section B - Architect Definition

N/A — customization and documentation-only story; no production code, layer, contract, or cross-cutting
concern is impacted.

### Layer Impact Matrix

`N/A` — non-runtime story; modifies `.github/agents/*.agent.md` and `docs/agentic-sdlc-workflow.md` only.

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

Extended the three concurrent reviewer agents and the workflow doc:

- `.github/agents/concurrent.reviewer.code.agent.md` — Steps 7–11: post-report FAIL prompt, apply fixes
  (hexagonal-architecture constraints from `docs/architecture.md`, present options when ambiguous), show
  `git status --short` + ask confirmation, commit `fix(review): address concurrent.reviewer.code findings
  ([N] file(s))` + push, instruct re-validation in new session. Added Rule: MUST NOT re-run the review in the
  same session after applying fixes.
- `.github/agents/concurrent.reviewer.documentation.agent.md` — Steps 6–10: same pattern with doc-specific
  guards (no editing prompts/instructions/agents; no meaning changes) and commit message
  `docs(review): address concurrent.reviewer.documentation findings ([N] file(s))`.
- `.github/agents/concurrent.reviewer.copilot.customization.agent.md` — Steps 7–11: same pattern, commit
  message `chore(review): address concurrent.reviewer.copilot.customization findings`. Removed obsolete rule
  forbidding modification of the target file.
- Deleted `.github/agents/code.fix.agent.md` and `.github/agents/documentation.fix.agent.md` (behavior
  superseded by reviewer post-report flow).
- `docs/agentic-sdlc-workflow.md` — inserted Phase 6 Release Code Review between `/complete-feature` and QA,
  scope `release/v<X.Y.Z>` diff vs `main`, same report → optional fix → confirm → commit → re-validate
  semantics as Phase 5. Renumbered QA → 7, Technical Writer → 8, Documentation Review → 9, Release Manager →
  10 across the ASCII diagram, Mermaid diagram, Phases in Detail prose, Quick Reference table, and Tips.

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `.github/agents/` and `docs/` only.

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

N/A — customization and documentation-only story; no cross-layer boundaries to exercise.

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
