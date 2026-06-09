# User Story Specification

## Metadata

- Story ID: `US-02`
- Owner: `3M0R4C`
- Status: `Done`

## Section A - Product Owner Definition

### Story Statement

As a `Tech Lead`, I want `the product_owner agent to drive release-centric
artifact creation (release, feature, then stories)`, so that `every new request
lands in the correct release folder with the new templates and gates`.

### Functional Requirements

- FR-01: The `product_owner` agent uses `docs/stories/_templates/_release.md`, `_feature.md`, and `_user_story.md` as
  the canonical templates
- FR-02: When a feature request targets a new release, the agent creates `docs/stories/v<X.Y.Z>/_release.md` from the
  release template
- FR-03: When a feature request targets a new feature, the agent creates
  `docs/stories/v<X.Y.Z>/<feature-slug>/_feature.md` from the feature template
- FR-04: When a feature request creates stories, the agent creates
  `docs/stories/v<X.Y.Z>/<feature-slug>/US-NN-<slug>.md` from the user story template
- FR-05: The agent never creates files under `docs/stories/done/` or `docs/stories/todo/`
- FR-06: The agent fills only Section A of each story and signs the Product Owner Handoff Gate
- FR-07: The agent updates the feature's `Stories` table and the release's `Features` table to reflect the created
  artifacts
- FR-08: The agent rejects requests that ambiguously target multiple releases

### Non-Functional Requirements

- Security: `N/A`
- Performance: `N/A`
- Observability: Agent reports each artifact created with its absolute path
- Reliability: Agent must be idempotent — re-running on the same input must not duplicate stories or features
- Compliance: All generated files must pass `markdownlint-cli2`

### Acceptance Criteria

- AC-01: Running `product_owner` for a new release+feature+stories produces a `_release.md`, `_feature.md`, and one or
  more `US-*.md` files under the correct release folder
- AC-02: Running `product_owner` for a new feature in an existing release does not modify `_release.md` except to add a
  row to the `Features` table
- AC-03: Running `product_owner` for new stories in an existing feature does not modify `_feature.md` except to add
  rows to the `Stories` table
- AC-04: The agent's `concurrent.reviewer.copilot.customization` review reports zero findings
- AC-05: All generated markdown passes `markdownlint-cli2` with zero errors

### Notes

- This is the highest-impact agent change; it owns the entry point of the SDLC

### Product Owner Handoff Gate

- Metadata set and follows naming conventions: `Yes`
- Story statement complete and outcome-focused: `Yes`
- FRs atomic and testable: `Yes`
- NFRs specified across required categories: `Yes`
- Acceptance criteria measurable and complete: `Yes`
- Ready for architecture handoff: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-31`

## Section B - Architect Definition

Not applicable. This story modifies a single agent customization file
(`.github/agents/product_owner.agent.md`); no architectural design, component
boundaries, or code interfaces are introduced. Implementation follows the
canonical structure defined in `.github/instructions/agents.instructions.md`.

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
- Architect sign-off: `N/A, 2026-06-01`

## Section C - Implementation Tracking

The `product_owner` agent was rewritten in place at
`.github/agents/product_owner.agent.md` to:

- Reference the new templates under `docs/stories/_templates/` (`_release.md`,
  `_feature.md`, `_user_story.md`, `README.md`).
- Resolve `release-version` from `target_release` input or the active
  `In Progress` `_release.md`, creating a release when ambiguous.
- Derive `feature-slug` and create or adopt
  `docs/stories/[release-version]/[feature-slug]/_feature.md`.
- Use `feat/[feature-slug]` branch naming (kebab-case with slash).
- Scaffold stories with `Status: Todo` using the new schema (Story ID, Owner,
  Status; no Request Date, Feature Slug, or Story Slug fields).
- Update both the feature's `Stories` table and the release's `Features
  Included` table on every run.
- Forbid any reference to `docs/stories/todo/` or `docs/stories/done/`.
- Forbid signing Technical Writer, QA, or Release Manager gates on
  `_release.md` (reserved for downstream agents).
- Emit the expanded Output Format covering release, feature, and story
  artifacts plus the updated handoff gate.

No production code changed; no unit or integration tests apply. Manual
validation: `npx markdownlint-cli2 .github/agents/product_owner.agent.md`
reports 0 errors.

### Test Traceability

`N/A` — non-runtime story; no acceptance criteria map to executable tests.

### Skeleton Inventory

`N/A` — no production stubs are created; implementation modifies `.github/agents/` only.

### Unit Tester Handoff Gate

- Every acceptance criterion has at least one test method: `N/A`
- Skeleton inventory complete and user-approved: `N/A`
- Tests compile and fail for the right reason (TDD red): `N/A`
- Ready for developer implementation: `Yes`
- Tester sign-off: `N/A, 2026-06-01`

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
- Developer sign-off: `3M0R4C, 2026-06-01`

## Section D - Integration Testing

Not applicable. The change is confined to an agent customization file and
produces no runtime behavior in the Camus API; there are no cross-layer
integrations to exercise.

### Integration Test Traceability

`N/A` — non-runtime story; no cross-layer boundaries to exercise.

### Integration Test Findings

`N/A` — no integration tests run.

### Integration Tester Handoff Gate

- All cross-layer boundaries identified and covered: `N/A`
- All integration tests pass: `N/A`
- No unresolved production code findings: `N/A`
- Ready for review: `Yes`
- Integration Tester sign-off: `N/A, 2026-06-01`
