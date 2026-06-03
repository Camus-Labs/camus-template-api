# Release Specification

## Metadata

- Target Date: `YYYY-MM-DD`
- Release Version: `vX.Y.Z`
- Release Type: `[MAJOR | MINOR | PATCH]`
- Status: `[In Progress | Ready for Deployment | Released]`

## Features

| Feature Slug | Title |
| --- | --- |
| `[feature-slug]` | [Feature title] |

## Notes

- [Optional: theme, deferred items, cross-feature deps, risks, manual migration or non-standard rollback — only when non-obvious]

## QA

### Test Suite

- Unit tests: `[pass_count]` passed, `[fail_count]` failed
- Integration tests: `[pass_count]` passed, `[fail_count]` failed
- Full suite: `[PASS | FAIL]`

### Coverage

- Files analyzed: `[count]`
- Files line coverage at 100%: `[count]`
- Files branch coverage at 100%: `[count]`
- Gaps closed: `[count]` file(s), `[test_added_count]` test(s) added, `[test_modified_count]` test(s) modified
- Gaps deferred: `[count]` file(s) (user decision)

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
- Swagger reflects all new/changed endpoints across stories: `[Yes | No | N/A]`
- Postman reflects all new/changed requests across stories: `[Yes | No | N/A]`
- XML documentation covers all new public APIs across stories: `[Yes | No | N/A]`
- Markdown linting passes with zero errors: `[Yes | No]`
- Build succeeds with zero errors and warnings: `[Yes | No]`
- Technical Writer sign-off: `[Name, Date]`

## Release Manager Handoff Gate

- All features in scope reached `Done` status: `[Yes | No]`
- Technical Writer sign-off complete: `[Yes | No]`
- QA sign-off complete: `[Yes | No]`
- Breaking changes captured in CHANGELOG: `[Yes | No | N/A]`
- PR `release/vX.Y.Z → main` opened and merged: `[Yes | No]`
- Tag `vX.Y.Z` created on `main`: `[Yes | No]`
- Release Manager sign-off: `[Name, Date]`
