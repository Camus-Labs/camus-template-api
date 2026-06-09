# Story Templates

Templates used by the agentic SDLC to specify releases, features, and user stories.

## Hierarchy

```text
docs/stories/
├── _templates/
│   ├── _release.md       Release-level specification
│   ├── _feature.md       Feature-level specification
│   └── _user_story.md    Story-level specification
├── next/                 Work-in-progress release (renamed to v<X.Y.Z> at release time)
│   ├── _release.md
│   └── <feature-slug>/
│       ├── _feature.md
│       └── US-<NN>-<slug>.md
└── v<X.Y.Z>/             Released or version-locked releases
    ├── _release.md
    └── <feature-slug>/
        ├── _feature.md
        └── US-<NN>-<slug>.md
```

## Lifecycle

- Product Owner seeds `next/_release.md` via the `ensure-on-release-branch` skill at Phase 0 (default version
  is the `next` placeholder mapped to `release/next`), then creates `<feature-slug>/_feature.md` and one or
  more `US-<NN>-<slug>.md` files per feature.
- Status is tracked **inside each document** via its Handoff Gate (no folder moves). Section D signed +
  `Status: Done` marks the story Done; release-level QA sign-off lives in `_release.md`.
- At Phase 8 the Technical Writer invokes `apply-release-version`, which renames `docs/stories/next/` to
  `docs/stories/v<X.Y.Z>/` and `release/next` to `release/v<X.Y.Z>` once the semantic version is confirmed.
- A release is shipped when its branch `release/v<X.Y.Z>` merges to `main`
  via the tag-triggered deployment pipeline.

## File Naming

- Release folder: `next` while the version is undecided, then `v<MAJOR>.<MINOR>.<PATCH>` (e.g., `v2.0.0`)
  after the Technical Writer applies the confirmed version.
- Feature folder: `<feature-slug>` in kebab-case.
- Story file: `US-<NN>-<story-slug>.md` where `NN` is zero-padded.

## Status Vocabulary

The `Metadata.Status` field of each document MUST use exactly one of the following values. Agent run-report
`Status:` headers (e.g. `READY`, `BLOCKED`, `DOCUMENTED`, `IMPLEMENTED`, `DONE`, `FAIL`) describe agent
outcomes and are distinct from these document statuses.

| Artifact | Allowed `Status` values |
| --- | --- |
| `_release.md` | `In Progress`, `Ready for Deployment`, `Released` |
| `_feature.md` | `Todo`, `In Progress`, `Done` |
| `US-<NN>-<slug>.md` | `Todo`, `In Progress`, `Done` |
