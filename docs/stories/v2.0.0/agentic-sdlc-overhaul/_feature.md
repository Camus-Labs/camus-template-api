# Feature Specification

## Metadata

- Request Date: `2026-05-31`
- Requested By: `3M0R4C`
- Owner: `3M0R4C`
- Status: `In Progress`

## Goal

Overhaul the agentic SDLC workflow so it operates around releases instead of
individual stories: introduce release-centric folder layout, split the single
story template into release/feature/story templates, update every SDLC agent to
match the new contracts, and document the end-to-end flow.

## Business Value

- Aligns the SDLC with versioning and deployment cadence (one release → many features → many stories)
- Eliminates duplicate gates (Technical Writer + QA now sign once per release, not once per story)
- Makes the changelog and the story tree consistent by construction
- Gives every agent a single source of truth for paths, templates, and gate expectations
- Reduces onboarding friction with `docs/agentic-sdlc-workflow.md`

## Stories

| Story ID | Title | Status |
| --- | --- | --- |
| `US-01` | Release-Centric Story Layout and Templates | `Done` |
| `US-02` | Update Product Owner Agent | `Planning` |
| `US-03` | Update Worker Agents Template Paths | `Planning` |
| `US-04` | Refactor QA Tester Agent for Story-Level Gates | `Planning` |
| `US-05` | Refactor Technical Writer Agent for Release Scope | `Planning` |
| `US-06` | Refactor Release Manager Agent for Tag and PR Flow | `Planning` |
| `US-07` | Update update-changelog Skill | `Planning` |
| `US-08` | Author Agentic SDLC Workflow Documentation | `Planning` |

## In Scope

- Three templates in `docs/stories/_templates/` (`_user_story.md`, `_feature.md`, `_release.md`) plus a README
- Migration of existing stories under `done/` to `v1.0.0/` and `v1.0.1/` release folders
- Removal of `docs/stories/done/` and `docs/stories/todo/`
- Updates to all eight SDLC agents and the `update-changelog` skill
- New `docs/agentic-sdlc-workflow.md` describing the end-to-end flow

## Out of Scope

- Changes to runtime code, HTTP contracts, or persistence schemas
- Automation of GitHub repository configuration (rulesets, environments, OIDC) — captured as a manual runbook
- Changes to non-SDLC agents or skills

## Cross-Story Dependencies

- `US-01` must complete before all other stories (provides the templates that the agents reference)
- `US-02` (`product_owner`) and `US-03` (worker agents) can proceed in parallel after `US-01`
- `US-04` (`tester.qa`) and `US-05` (`technical_writer`) can proceed in parallel after `US-01`
- `US-06` (`release_manager`) depends on `US-04` and `US-05` (release gates must exist first)
- `US-07` (`update-changelog` skill) depends on `US-05` (technical writer drives changelog now)
- `US-08` (workflow doc) depends on `US-02` through `US-07` (documents the final agent contracts)

## Feature-Level Constraints

- No runtime, HTTP, or schema changes ship in this feature
- All agent customization files must pass `concurrent.reviewer.copilot.customization` review
- All markdown files must pass `markdownlint-cli2` with zero errors
- All agent path references must use the new template locations under `docs/stories/_templates/`

## Open Questions

- None

## Product Owner Handoff Gate

- Metadata complete: `Yes`
- Goal stated as outcome (not implementation): `Yes`
- All stories created under this feature folder: `Yes`
- Cross-story dependencies identified: `Yes`
- Ready for development: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-31`
