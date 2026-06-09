---
name: ensure-on-release-branch
description: 'Verify a clean working tree and checkout the release branch for the supplied version to position the caller for release-level edits.'
argument-hint: 'Provide release_file=docs/stories/<next|vX.Y.Z>/_release.md or omit to default to release/next'
user-invocable: false
---

# Ensure On Release Branch

## When to Use

- Position the Product Owner, QA Tester, Technical Writer, Release Manager, or `complete-feature` skill on the
  release branch before any release-scope file edit.
- Scaffold a new `_release.md` when the release folder does not exist yet.
- Invoke when any release-scope agent needs canonical `release_version`, `release_branch`, `release_folder`, and
  `release_file` values derived from an optional `release_file` path before performing release operations.

## Procedure

1. Resolve `release_version` — when the caller supplied `release_file`, extract `release_version` from the path
   segment immediately under `docs/stories/` and validate it matches
   `^next$|^v[0-9]+\.[0-9]+\.[0-9]+$`; when the caller omitted `release_file`, default `release_version` to
   the placeholder `next`; if validation fails, return `FAIL` with `reason: "release_version invalid"`;
   otherwise proceed.
2. Assert a clean working tree — run `git status --porcelain` and if the output is non-empty return `FAIL`
   with `reason: "working tree must be clean to switch branch"`; otherwise run `git fetch origin` and proceed.
3. Checkout the release branch — set `release_branch="release/$release_version"` (use `release/next` for the
   `next` placeholder and `release/v<X.Y.Z>` for concrete versions); if
   `git show-ref --verify --quiet "refs/heads/$release_branch"` succeeds run
   `git checkout "$release_branch" && git pull --ff-only`; else if
   `git show-ref --verify --quiet "refs/remotes/origin/$release_branch"` succeeds run
   `git checkout -b "$release_branch" "origin/$release_branch"`; else if `release_version` equals `next` run
   `git checkout main && git pull && git checkout -b "$release_branch"`; else return `FAIL` with
   `reason: "release branch $release_branch does not exist on local or origin — invoke
   apply-release-version to rename release/next first"`.
4. Scaffold the release folder when missing — if `docs/stories/$release_version/_release.md` does not exist, run
   `mkdir -p "docs/stories/$release_version"` and copy `docs/stories/_templates/_release.md` to
   `docs/stories/$release_version/_release.md`, replace `Target Date: YYYY-MM-DD` with today's date from
   `date +%Y-%m-%d` and the `Status` line with `Status: In Progress`; leave the scaffold staged in the
   working tree for the caller to commit via `commit-and-push-on-release-branch`; otherwise proceed to Step 5.
5. Return `SUCCESS` with `release_version`, `release_branch`, `release_folder`
   (`docs/stories/$release_version/`), `release_file` (`docs/stories/$release_version/_release.md`), and a
   `notes` list stating the branch resolution method and scaffold creation status.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  release_version: [release_version: string]
  release_branch: [release_branch: string]
  release_folder: [release_folder: string]
  release_file: [release_file: string]
  notes:
    - [note: string]
```

```yaml
FAIL:
  release_version: [release_version_or_missing: string]
  reason: [reason: string]
```

## Dependencies

- `git` — version-control CLI; use to fetch refs and create or check out branches.
- `date` — POSIX date utility; use to produce today's date for scaffold substitutions.
- `mkdir` — POSIX directory-creation utility; use to create the release folder when scaffolding.
- `docs/stories/_templates/_release.md` — release file template; copy into the new release folder when scaffolding.
