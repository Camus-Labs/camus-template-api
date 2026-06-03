---
name: update-changelog
description: 'Summarize implemented changes from every story in a release and write a single release entry to CHANGELOG.md.'
argument-hint: 'Provide the path to a _release.md file (e.g., "docs/stories/v2.0.0/_release.md")'
user-invocable: false
---

# Update Changelog

## When to Use

- Record a single changelog entry when a release reaches the technical writer phase.
- Consolidate every story in the release into one `## [vX.Y.Z] - YYYY-MM-DD` section.
- Determine the release `Version Update` subsection content (previous version, new version, bump type, reason).

## Procedure

1. Validate `release_path` exists and points to a `_release.md` file — if missing, empty, or not named
   `_release.md`, return `FAIL` with `reason: "missing or invalid release_path argument"`; otherwise proceed.
2. Enumerate every `US-*.md` file under the same release folder; for each story, read the Story Statement and
   Functional Requirements from Section A and summarize each implemented change as one imperative sentence
   (e.g., "Add token revocation endpoint"); group entries under `Added`, `Changed`, `Fixed`, `Removed`,
   `Security`, or `Deprecated` subsections — omit empty subsections.
3. Simplify each entry to a minimal, human-readable description — remove implementation details, class names,
   and technical jargon; focus on what changed from a user or API consumer perspective.
4. Read `src/Directory.Build.props` for the current version and derive `bump_type` from the grouped entries
   produced in Step 3 — MAJOR when any entry describes a breaking API change, otherwise MINOR when `Added`
   or `Changed` contains user-visible features or endpoints, otherwise PATCH; compute the proposed new
   version by applying `bump_type` to the current version.
5. Ask the user to confirm the computed version and bump type; retry once if unclear — if still no answer,
   return `FAIL` with `reason: "no confirmed version choice"`; otherwise proceed with the confirmed version.
6. Update `src/Directory.Build.props` `<Version>` to the confirmed version and insert a new
   `## [vX.Y.Z] - <today>` section above the latest release in `CHANGELOG.md` containing every grouped entry from
   Step 3.
7. Return `SUCCESS` with the structured output containing the versions, bump type, and the changelog lines written.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  version: [target_version: string]
  previous_version: [previous_version: string]
  bump_type: [bump_type: enum(MAJOR, MINOR, PATCH)]
  changelog_lines: |
    [exact_markdown_lines_inserted: string]
```

```yaml
FAIL:
  reason: [failure_description: string]
```

## Dependencies

- `CHANGELOG.md` — existing changelog file at workspace root
- `src/Directory.Build.props` — canonical version property file
- `CONTRIBUTING.md` — versioning standard reference (Semantic Versioning rules)
- `_release.md` — release specification file whose folder contains the `US-*.md` files
