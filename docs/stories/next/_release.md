# Release Specification

## Metadata

- Target Date: `2026-06-15`
- Release Version: `next`
- Release Type: `MAJOR`
- Status: `In Progress`

## Features

| Feature Slug | Title |
| --- | --- |
| `release-centric-sdlc` | Release-Centric Agentic SDLC Overhaul |

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

- Previous version: `[X.Y.Z]`
- New version: `[X.Y.Z]`
- Bump type: `[MAJOR | MINOR | PATCH]`
- Reason: `[one-sentence justification]`

### CHANGELOG Entry

```markdown
## [X.Y.Z] - YYYY-MM-DD

### `[Added | Changed | Deprecated | Removed | Fixed | Security]`

- [entry 1]
- [entry 2]
```

### Documentation Updates

- Swagger annotations updated: `[count]` endpoint(s)
- Postman requests updated: `[count]` request(s)
- XML documentation added: `[count]` public API(s)

### Technical Writer Handoff Gate

- Version in `Directory.Build.props` matches `vX.Y.Z`: `[Yes | No]`
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
- PR `release/vX.Y.Z → main` opened and merged: `[Yes | No]`
- Tag `vX.Y.Z` created on `main`: `[Yes | No]`
- Release Manager sign-off: `[Name, Date]`
