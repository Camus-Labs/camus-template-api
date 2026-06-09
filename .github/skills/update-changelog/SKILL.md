---
name: update-changelog
description: 'Summarize implemented changes from every story in a release, compute the proposed semantic version bump, and write a draft entry to CHANGELOG.md for the caller to review before applying the version.'
argument-hint: 'Provide release_file (path to the in-flight release `_release.md`, always "docs/stories/next/_release.md")'
user-invocable: false
---

# Update Changelog

## When to Use

- Draft a single changelog entry when a release reaches the technical writer phase.
- Consolidate every story in the release into one `## [X.Y.Z] - YYYY-MM-DD` section in `CHANGELOG.md`.
- Compute and propose a semantic version bump (previous version, proposed version, bump type, reason) for
  caller review — without touching `Directory.Build.props`, `_release.md`, or the release folder/branch name.
- Pair with `apply-release-version` after the caller obtains the user-confirmed version.

## Procedure

1. Validate `release_file` exists and points to a `_release.md` file — if missing, empty, or not named
   `_release.md`, return `FAIL` with `reason: "missing or invalid release_file argument"`; otherwise proceed.
2. Enumerate every `US-*.md` file under the same release folder; for each story, read the Story Statement and
   Functional Requirements from Section A and summarize each implemented change as one imperative sentence
   (e.g., "Add token revocation endpoint"); group entries under `Added`, `Changed`, `Fixed`, `Removed`,
   `Security`, or `Deprecated` subsections — omit empty subsections.
3. Simplify each entry to a minimal, human-readable description — remove implementation details, class names,
   and technical jargon; focus on what changed from a user or API consumer perspective.
4. Read `src/Directory.Build.props` for the current `previous_version` and derive `proposed_bump_type` from
   the grouped entries produced in Step 3 — MAJOR when any entry describes a breaking API change, otherwise
   MINOR when
   `Added` or `Changed` contains user-visible features or endpoints, otherwise PATCH; compute
   `proposed_version` by applying `proposed_bump_type` to `previous_version` and capture a one-sentence
   `proposed_bump_reason` referencing the entries that drove the choice.
5. Insert a new `## [$proposed_version] - YYYY-MM-DD` section above the latest release entry in
   `CHANGELOG.md` containing every grouped entry from Step 3 (`$proposed_version` written without a leading
   `v` to match the `## [X.Y.Z]` format enforced by `build-version-check.yml`); on filesystem failure,
   return `FAIL` with `reason: "changelog write failed"` and the error.
6. Return `SUCCESS` with `proposed_version`, `previous_version`, `proposed_bump_type`,
   `proposed_bump_reason`, and the exact `changelog_lines` written so the caller can present the draft to
   the user and decide whether to accept the proposed version or supply a `user_confirmed_version` before
   invoking `apply-release-version`.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  proposed_version: [proposed_version: string]
  previous_version: [previous_version: string]
  proposed_bump_type: [proposed_bump_type: enum(MAJOR, MINOR, PATCH)]
  proposed_bump_reason: [proposed_bump_reason: string]
  changelog_lines: |
    [exact_markdown_lines_inserted: string]
```

```yaml
FAIL:
  reason: [failure_description: string]
  error: [error_detail: string]
```

## Dependencies

- `CHANGELOG.md` — workspace-root changelog file; insertion target for the draft section.
- `src/Directory.Build.props` — canonical version property file (read-only here).
- `CONTRIBUTING.md` — versioning standard reference (Semantic Versioning rules).
- `_release.md` — release specification file; read sibling `US-*.md` story files from its parent folder (read-only here).
- `apply-release-version` — sibling skill for persisting the user-confirmed version (invoked by the caller next).
- `build-version-check.yml` — CI workflow enforcing the `## [X.Y.Z]` changelog heading format (read-only reference).
