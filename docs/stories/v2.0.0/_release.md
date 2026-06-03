# Release Specification

## Metadata

- Target Date: `2026-06-15`
- Release Version: `v2.0.0`
- Release Type: `MAJOR`
- Status: `In Progress`

## Features

| Feature Slug | Title |
| --- | --- |
| `agentic-sdlc-overhaul` | Release-Centric Agentic SDLC Overhaul |

## Notes

- MAJOR bump because the agentic SDLC workflow (templates, agent contracts,
  branching model, release artifacts) is reorganized in a non-backward-compatible
  way: existing in-flight stories under the old layout cannot continue under the
  new agent contracts without migration.
- No runtime or HTTP-contract changes ship in this release; the API surface is
  unchanged from `v1.0.1`.
- GitHub UI configuration (rulesets, environments, OIDC) is captured in this
  release's documentation but performed manually by the Release Manager.

## QA

### Test Suite

- Unit tests: `[pass_count]` passed, `[fail_count]` failed
- Integration tests: `[pass_count]` passed, `[fail_count]` failed
- Full suite: `[PASS | FAIL]`

### Coverage

- Files analyzed: `0`
- Files line coverage at 100%: `0`
- Files branch coverage at 100%: `0`
- Gaps closed: `0` file(s), `0` test(s) added, `0` test(s) modified
- Gaps deferred: `0` file(s) (user decision)

### Local Validation

- User confirmed: `[Yes | No | Skipped]`
- Issues reported: `[description or "None"]`

### QA Handoff Gate

- All stories in scope have Sections A–D signed off and `Status: Done`: `[Yes | No]`
- Full test suite passes: `[Yes | No]`
- Coverage gaps addressed or acknowledged: `[Yes | No]`
- Local validation confirmed: `[Yes | No]`
- QA sign-off: `[Name, Date]`

## Technical Writer

### Version Update

- Previous version: `1.0.1`
- New version: `2.0.0`
- Bump type: `MAJOR`
- Reason: Breaking redesign of the agentic SDLC workflow and story/release layout

### CHANGELOG Entry

```markdown
## [2.0.0] - 2026-06-15

### Changed

- **BREAKING (SDLC):** Reorganized `docs/stories/` from `todo/done` flat layout to release-centric `v<X.Y.Z>/<feature>/US-*.md` layout
- **BREAKING (SDLC):** Replaced single `_user_story_template.md` with three templates under `docs/stories/_templates/` (`_user_story.md`, `_feature.md`, `_release.md`)
- Simplified user story template to four sections (A–D); release-level concerns (Technical Writer, QA, Release Manager) moved to `_release.md`
- Updated `product_owner` agent to create `_release.md`, `_feature.md`, and stories under the release-centric layout
- Updated `architect`, `developer`, `tester.unit`, `tester.integration` agents to reference new template paths
- Refactored `tester.qa` agent to sign story-level Sections A–D and contribute to the release QA section
- Refactored `technical_writer` agent to operate at release scope and populate the `_release.md` Technical Writer section
- Refactored `release_manager` agent to drive tag creation and `release/v<X.Y.Z> → main` PR flow
- Updated `update-changelog` skill for release-level operation

### Added

- `docs/agentic-sdlc-workflow.md` describing the end-to-end SDLC flow, agents, gates, and branching model
- `docs/stories/_templates/README.md` explaining the release → feature → story hierarchy and lifecycle

### Removed

- `docs/stories/done/` and `docs/stories/todo/` folders (replaced by release-centric layout)
- Sections E (Technical Writer) and F (QA) from the user story template (moved to release)
```

### Documentation Updates

- Swagger annotations updated: `0` endpoint(s)
- Postman requests updated: `0` request(s)
- XML documentation added: `0` public API(s)

### Technical Writer Handoff Gate

- Version in `Directory.Build.props` matches `v2.0.0`: `[Yes | No]`
- CHANGELOG entry consolidates all features in this release: `[Yes | No]`
- Release `Features` table matches current feature folders: `[Yes | No]`
- Each `_feature.md` `Stories` table matches its `US-*.md` files and gate status: `[Yes | No]`
- Swagger reflects all new/changed endpoints across stories: `N/A`
- Postman reflects all new/changed requests across stories: `N/A`
- XML documentation covers all new public APIs across stories: `N/A`
- Markdown linting passes with zero errors: `[Yes | No]`
- Build succeeds with zero errors and warnings: `[Yes | No]`
- Technical Writer sign-off: `[Name, Date]`

## Release Manager Handoff Gate

- All features in scope reached `Done` status: `[Yes | No]`
- Technical Writer sign-off complete: `[Yes | No]`
- QA sign-off complete: `[Yes | No]`
- Breaking changes captured in CHANGELOG: `Yes`
- PR `release/v2.0.0 → main` opened and merged: `[Yes | No]`
- Tag `v2.0.0` created on `main`: `[Yes | No]`
- Release Manager sign-off: `[Name, Date]`
