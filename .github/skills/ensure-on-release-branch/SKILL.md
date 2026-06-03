---
name: ensure-on-release-branch
description: 'Verify a clean working tree and checkout the release branch for the supplied version to position the caller for release-level edits.'
argument-hint: 'Optional release_version'
user-invocable: false
---

# Ensure On Release Branch

## When to Use

- Position the Product Owner, QA Tester, Technical Writer, Release Manager, or `complete-feature` skill on the
  release branch before any release-scope file edit.
- Scaffold a new `_release.md` when the release folder does not exist yet.

## Procedure

1. Default `release_version` to `vX.Y.Z` when the caller omits it, then validate it matches
   `^vX\.Y\.Z$|^v[0-9]+\.[0-9]+\.[0-9]+$` — if not, return `FAIL` with `reason: "release_version invalid"`;
   otherwise run `git status --porcelain` and if the output is non-empty return `FAIL` with `reason:
   "working tree must be clean to switch branch"`; otherwise run `git fetch origin` and proceed.
2. Map the release branch by setting `release_branch="release/$release_version"` (use `release/next` when
   `release_version` equals `vX.Y.Z`); ensure the branch exists locally and is up to date — if
   `git show-ref --verify --quiet "refs/heads/$release_branch"` succeeds run
   `git checkout "$release_branch" && git pull --ff-only`; else if
   `git show-ref --verify --quiet "refs/remotes/origin/$release_branch"` succeeds run
   `git checkout -b "$release_branch" "origin/$release_branch"`; else run
   `git checkout main && git pull && git checkout -b "$release_branch" && git push -u origin "$release_branch"`.
3. Scaffold the release folder when missing — if `docs/stories/$release_version/_release.md` does not exist, run
   `mkdir -p "docs/stories/$release_version"` and copy `docs/stories/_templates/_release.md` to
   `docs/stories/$release_version/_release.md`, replace `Target Date: YYYY-MM-DD` with `target_date` (default
   to today via `date +%Y-%m-%d` when caller omitted it) and the `Status` line with `Status: In Progress`,
   then run `git add "docs/stories/$release_version/_release.md" && git commit -m "chore: scaffold
   $release_version release tracking" && git push origin "$release_branch"`; otherwise skip.
4. Return `SUCCESS` with `release_version`, `release_branch`, `release_folder`
   (`docs/stories/$release_version/`), `release_file` (`docs/stories/$release_version/_release.md`), and a
   `notes` list describing whether the branch was found locally, tracked from origin, or newly created and
   whether the release scaffold was created.

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

- `git` — version-control CLI; fetches refs and creates or checks out branches.
- `date` — POSIX date utility; produces today's date for scaffold substitutions.
- `docs/stories/_templates/_release.md` — release file template copied when scaffolding a new release.
