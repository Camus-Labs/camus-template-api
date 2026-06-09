---
name: apply-release-version
description: 'Persist a user-confirmed release version across Directory.Build.props, _release.md metadata, the next-placeholder release folder, and the release/next branch to lock all version-bearing artifacts at release time.'
argument-hint: 'Provide release_file and confirmed_version in semver form (X.Y.Z, without leading v)'
user-invocable: false
---

# Apply Release Version

## When to Use

- Persist the version the user confirmed after reviewing the draft entry produced by `update-changelog`.
- Rename the release folder and branch from the `next` placeholder to the concrete `v<X.Y.Z>` at release time.
- Keep `src/Directory.Build.props` and `_release.md` metadata consistent with the confirmed version.

## Procedure

1. Validate inputs — `confirmed_version` must match `^[0-9]+\.[0-9]+\.[0-9]+$` (semver form without
   leading `v`); return `FAIL` with `reason: "confirmed_version invalid"` otherwise; `release_file` must
   exist and end in `_release.md`, otherwise return `FAIL` with `reason: "missing or invalid release_file"`;
   verify that the `CHANGELOG.md` draft header already reflects `confirmed_version` — if not, return
   `FAIL` with `reason: "CHANGELOG.md draft header does not match confirmed_version"`;
   otherwise set `versioned_ref="v$confirmed_version"` (the form used for git branch, folder, and tag
   refs) and proceed.
2. Derive current state — capture `current_folder_version` from the path segment immediately under
   `docs/stories/` in `release_file` and `current_branch` from `git rev-parse --abbrev-ref HEAD`; if
   `current_folder_version` is not `next`, return `FAIL` with `reason: "release folder is not on next
   placeholder"` and `details: $current_folder_version`; if `current_branch` does not match `^release/`,
   return `FAIL` with `reason: "not on a release branch — invoke ensure-on-release-branch first"` and
   `details: $current_branch`; if `current_branch` is not `release/next`, return `FAIL` with `reason:
   "release branch is not release/next"` and `details: $current_branch`; otherwise proceed.
3. Update `src/Directory.Build.props` — replace the existing `<Version>` element value with
   `$confirmed_version`; on filesystem failure, return `FAIL` with `reason:
   "Directory.Build.props write failed"` and the error.
4. Update `_release.md` metadata — replace the `Release Version:` value with `$confirmed_version` so the
   metadata matches the semver form used in `Directory.Build.props`, `CHANGELOG.md`, and the Technical
   Writer `Version Update` subsection; on filesystem failure, return `FAIL` with `reason: "release_file
   write failed"` and the error.
5. Rename the release folder to align it with the confirmed version — run
   `git mv "docs/stories/next" "docs/stories/$versioned_ref"`; on failure, return `FAIL` with `reason:
   "folder rename failed"` and the git error.
6. Rename the release branch to align it with the renamed folder — first run
   `gh pr list --base release/next --state open --json number,headRefName,url`; if the result is a
   non-empty array, return `FAIL` with `reason: "open PRs target release/next — close or merge before
   renaming"` and `details` containing the JSON array so the caller can list each PR; otherwise run
   `git fetch origin` and `git diff --quiet HEAD "origin/release/next"`; if the diff is non-empty, return
   `FAIL` with `reason: "release/next on origin diverged from local — pull and re-run the
   technical_writer flow"` and `details` capturing `git rev-parse HEAD` and
   `git rev-parse origin/release/next`; otherwise run
   `git branch -m "release/next" "release/$versioned_ref"`, then
   `git push -u origin "release/$versioned_ref"`, then
   `git push origin --delete release/next`; on any git failure, return `FAIL` with `reason: "branch rename
   failed"` and the git error.
7. Return `SUCCESS` with `release_version` (always `$versioned_ref`), `release_branch`
   (`release/$versioned_ref`), `release_folder` (`docs/stories/$versioned_ref/`), and `release_file`
   (`docs/stories/$versioned_ref/_release.md`).

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  release_version: [release_version: string]
  release_branch: [release_branch: string]
  release_folder: [release_folder: string]
  release_file: [release_file: string]
```

```yaml
FAIL:
  reason: [failure_description: string]
  details: [diagnostic_output_or_absent: optional string]
```

## Dependencies

- `git` — version-control CLI used for `git mv`, `git branch -m`, branch push, and branch delete.
- `gh` — GitHub CLI; query open PRs targeting `release/next` before deleting the placeholder branch.
- `ensure-on-release-branch` — prerequisite skill the caller must invoke to position the working tree on a release branch.
- `src/Directory.Build.props` — canonical version property file written with the numeric form of the version.
- `CHANGELOG.md` — release changelog whose draft header must already reflect `confirmed_version` before this skill runs.
- `_release.md` — release specification whose `Release Version` metadata is written.
- `update-changelog` — upstream skill producing the draft changelog entry; invoke before this skill.
