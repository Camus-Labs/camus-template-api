# Story Templates

Templates used by the agentic SDLC to specify releases, features, and user stories.

## Hierarchy

```text
docs/stories/
├── _templates/
│   ├── _release.md       Release-level specification
│   ├── _feature.md       Feature-level specification
│   └── _user_story.md    Story-level specification
└── v<X.Y.Z>/
    ├── _release.md
    └── <feature-slug>/
        ├── _feature.md
        └── US-<NN>-<slug>.md
```

## Lifecycle

- Release Manager seeds `v<X.Y.Z>/_release.md` when planning starts.
- Product Owner creates `<feature-slug>/_feature.md` and one or more
  `US-<NN>-<slug>.md` files per feature.
- Status is tracked **inside each document** via its Handoff Gate
  (no folder moves). Completion of Section F (QA sign-off) marks a story Done.
- A release is shipped when its branch `release/v<X.Y.Z>` merges to `main`
  via the tag-triggered deployment pipeline.

## File Naming

- Release folder: `v<MAJOR>.<MINOR>.<PATCH>` (e.g., `v2.0.0`).
- Feature folder: `<feature-slug>` in kebab-case.
- Story file: `US-<NN>-<story-slug>.md` where `NN` is zero-padded.
