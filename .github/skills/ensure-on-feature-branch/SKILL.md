---
name: ensure-on-feature-branch
description: 'Verify a clean working tree and checkout the feature branch off release/next to position worker agents for story implementation edits.'
argument-hint: 'Provide feature_slug (lowercase kebab-case)'
user-invocable: false
---

# Ensure On Feature Branch

## When to Use

- Position worker agents (architect, tester.unit, developer, tester.integration) on the correct
  `feat/<feature-slug>` branch before editing Section B, C, or D of any story file.
- Create the feature branch off `release/next` when no prior worker has run yet.

## Procedure

1. Validate `feature_slug` matches `^[a-z0-9]+(-[a-z0-9]+)*$` — if not, return `FAIL` with `reason:
   "feature_slug missing or invalid"`; otherwise proceed.
2. Assert a clean working tree — run `git status --porcelain` and if the output is non-empty return `FAIL`
   with `reason: "working tree must be clean to switch branch"`; otherwise run `git fetch origin` and proceed.
3. Confirm the feature is scaffolded on the `next` placeholder — run
   `find docs/stories -mindepth 3 -maxdepth 3 -type f -path "*/$feature_slug/_feature.md" 2>/dev/null` and
   inspect the matches:
    - on zero matches, return `FAIL` with `reason: "feature $feature_slug not scaffolded — Product Owner
      must create it first"`;
    - on multiple matches, return `FAIL` with `reason: "feature $feature_slug exists in multiple releases"`;
    - on a single match whose release segment is not `next`, return `FAIL` with `reason: "feature
      $feature_slug belongs to release $found_segment which is already finalized — story implementation must
      occur on release/next before technical_writer renames the release"`;
    - on a single match under `next`, proceed.
4. Set `release_version="next"` and `release_branch="release/next"` — treat both as fixed constants.
   Verify `git rev-parse --verify "origin/$release_branch"`
   succeeds; on failure, return `FAIL` with `reason: "release branch release/next not found on origin — Product
   Owner must scaffold the release first"`; otherwise proceed.
5. Set `feature_branch="feat/$feature_slug"` and ensure it is checked out from the release branch — if
   `git show-ref --verify --quiet "refs/heads/$feature_branch"` succeeds run
   `git checkout "$feature_branch" && git pull --ff-only`; else if
   `git show-ref --verify --quiet "refs/remotes/origin/$feature_branch"` succeeds run
   `git checkout -b "$feature_branch" "origin/$feature_branch"`; else run
   `git checkout "$release_branch" && git pull --ff-only && git checkout -b "$feature_branch" && git push -u
   origin "$feature_branch"`.
6. Return `SUCCESS` with `feature_slug`, `release_version` (always `next`), `release_branch` (always
   `release/next`), `feature_branch`, `feature_folder` (`docs/stories/next/$feature_slug/`), and a `notes`
   list — include in `notes` whether the branch was found locally, tracked from origin, or created fresh.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  feature_slug: [feature_slug: string]
  release_version: "next"
  release_branch: "release/next"
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

- `git` — version-control CLI; use to fetch refs and check out branches.
- `find` — POSIX file search; use to locate the existing `_feature.md` and derive the release.
