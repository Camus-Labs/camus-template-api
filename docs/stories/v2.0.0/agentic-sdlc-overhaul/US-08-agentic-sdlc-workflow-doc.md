# User Story Specification

## Metadata

- Story ID: `US-08`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `new contributor to this repository`, I want `a single document that
describes the agentic SDLC end-to-end (templates, agents, gates, branching,
release flow)`, so that `I can understand and operate the workflow without
reading every agent file`.

### Functional Requirements

- FR-01: `docs/agentic-sdlc-workflow.md` exists at the repository root of the docs tree
- FR-02: The document describes the release → feature → story hierarchy and the corresponding folder layout under
  `docs/stories/`
- FR-03: The document lists every SDLC agent (`product_owner`, `architect`, `tester.unit`, `developer`,
  `tester.integration`, `tester.qa`, `technical_writer`, `release_manager`) with its input, output, and gate
- FR-04: The document describes the branching model (`main`, `release/v<X.Y.Z>`, `feat/*`, `fix/*`, `chore/*`,
  `docs/*`, `hotfix/*`) and the merge strategies (squash for `feat → release`, rebase for `release → main`)
- FR-05: The document includes a high-level diagram (mermaid) of the agent flow from feature request to released tag
- FR-06: The document links to each template under `docs/stories/_templates/`
- FR-07: The document describes the manual GitHub UI configuration required (rulesets for `main` and `release/*`,
  `development` and `production` environments, OIDC secrets) as a runbook section
- FR-08: The README of the repository links to this document

### Non-Functional Requirements

- Security: GitHub UI runbook references secret names and OIDC subjects but never contains secret values
- Performance: `N/A`
- Observability: `N/A`
- Reliability: Document must remain in sync with agent files (verified manually at release time)
- Compliance: `markdownlint-cli2` passes

### Acceptance Criteria

- AC-01: `docs/agentic-sdlc-workflow.md` exists and contains all eight agents
- AC-02: The document contains a mermaid diagram of the agent flow
- AC-03: The document contains a `GitHub UI Configuration` section
- AC-04: `README.md` contains a link to `docs/agentic-sdlc-workflow.md`
- AC-05: `markdownlint-cli2` passes on the new file and on `README.md`

### Notes

- Depends on US-02 through US-07 (the agents must be in their final form before
  documenting them)

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-31`

## Section B - Architect Definition

N/A — documentation-only story; no production code, layer, contract, or cross-cutting concern is impacted.

### Layer Impact Matrix

`N/A` — non-runtime story; documentation only. No runtime layers are affected.

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

Rewrote `docs/agentic-sdlc-workflow.md` end-to-end to reflect the v2.0.0 SDLC contracts:

- Documented the branch model (`main ← release/v<X.Y.Z> ← feat/<slug>`) and merge strategies (squash for
  `feat → release`, rebase for `release → main`, tag on `main`).
- Documented the artifact layout (`docs/stories/v<X.Y.Z>/<feature-slug>/`) and the 5-gate model (A–D per story
  plus QA/TW/RM per release plus the feature gate signed by `/complete-feature`).
- Listed all SDLC phases with the agent invoked, the input, the output, and the gate; including the new
  `concurrent.reviewer.code` (per-story, after integration tests) and `concurrent.reviewer.documentation`
  (per-release, after technical writer) phases.
- Documented the `ensure-on-release-branch`, `ensure-on-feature-branch`, and `complete-feature` skills.
- Updated the Quick Reference table and the Tips section to match the new flow.
- Added a Mermaid diagram (`flowchart TD` with nested `STORY`/`FEATURE`/`RELEASE` subgraphs) rendering the
  nested cycles end-to-end (FR-05).
- Added the `GitHub UI Configuration` runbook section covering rulesets for `main` and `release/*`,
  `development`/`production` environments, and the OIDC subject claim format (FR-07).
- Added a link to `docs/agentic-sdlc-workflow.md` from `README.md` (FR-08).

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `docs/` only.

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

N/A — documentation-only story; no cross-layer boundaries to exercise.

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
