---
name: ensure-on-feature-branch
description: 'Verify a clean working tree and checkout the feature branch off the release branch to position worker agents for story implementation edits.'
argument-hint: 'Provide feature_slug (lowercase kebab-case)'
user-invocable: false
---

# Ensure On Feature Branch

## When to Use

- Position worker agents (architect, tester.unit, developer, tester.integration) on the correct
  `feat/<feature-slug>` branch before editing Section B, C, or D of any story file.
- Create the feature branch off the feature's release branch when no prior worker has run yet.

## Procedure

1. Validate `feature_slug` matches `^[a-z0-9]+(-[a-z0-9]+)*$` — if not, return `FAIL` with `reason:
   "feature_slug missing or invalid"`; otherwise run `git status --porcelain` and if the output is non-empty
   return `FAIL` with `reason: "working tree must be clean to switch branch "`; otherwise run `git fetch origin`
   and proceed.
2. Derive `release_version` by running `ls -d docs/stories/v*/"$feature_slug"/_feature.md 2>/dev/null` and
   taking the `v*` path segment of the single match — on zero matches, return `FAIL` with `reason: "feature
   $feature_slug not scaffolded — Product Owner must create it first"`; on multiple matches, return `FAIL`
   with `reason: "feature $feature_slug exists in multiple releases"`; otherwise proceed.
3. Map the release branch by setting `release_branch="release/next"` when `release_version` equals `vX.Y.Z`
   or `release_branch="release/$release_version"` otherwise, then verify
   `git rev-parse --verify "origin/$release_branch"` succeeds — on failure, return `FAIL` with `reason:
   "release branch $release_branch not found on origin — Product Owner must scaffold the release first"`;
   otherwise proceed.
4. Set `feature_branch="feat/$feature_slug"` and ensure it is checked out from the release branch — if
   `git show-ref --verify --quiet "refs/heads/$feature_branch"` succeeds run
   `git checkout "$feature_branch" && git pull --ff-only`; else if
   `git show-ref --verify --quiet "refs/remotes/origin/$feature_branch"` succeeds run
   `git checkout -b "$feature_branch" "origin/$feature_branch"`; else run
   `git checkout "$release_branch" && git pull --ff-only && git checkout -b "$feature_branch" && git push -u
   origin "$feature_branch"`.
5. Return `SUCCESS` with `feature_slug`, `release_version`, `release_branch`, `feature_branch`,
   `feature_folder` (`docs/stories/$release_version/$feature_slug/`), and a `notes` list describing whether
   the feature branch was found locally, tracked from origin, or newly created.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  feature_slug: [feature_slug: string]
  release_version: [release_version: string]
  release_branch: [release_branch: string]
  feature_branch: [feature_branch: string]
  feature_folder: [feature_folder: string]
  notes:
    - [note: string]
```

```yaml
FAIL:
  feature_slug: [feature_slug_or_missing: string]
  reason: [reason: string]
```

## Dependencies

- `git` — version-control CLI; fetches refs and creates or checks out branches.
- `ls` — POSIX directory listing; locates the existing `_feature.md` to derive the release.
