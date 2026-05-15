---
name: update-changelog
description: 'Summarize implemented changes from a resolved user story and append entries to the changelog, creating a new release section or appending to an existing one.'
argument-hint: 'Provide the path to a resolved user story file (e.g., "docs/stories/US-001.md")'
user-invocable: false
---

# Update Changelog

## When to Use

- Record changelog entries when a user story reaches the documentation phase.
- Determine the correct version bump and record release notes for a completed story.
- Append entries to an existing version section without creating a duplicate header.

## Procedure

1. Validate `story_path` exists — if missing or empty, return `FAIL` with
   `reason: "missing or empty story_path argument"`; otherwise proceed to step 2.
2. Read the story file and summarize each implemented change as one imperative sentence
   (e.g., "Add token revocation endpoint"); group under `Added`, `Changed`, `Fixed`, `Removed`,
   `Security`, or `Deprecated` subsections — omit empty ones.
3. Simplify each entry to a minimal, human-readable description — remove implementation details,
   class names, and technical jargon; focus on what changed from a user or API consumer perspective
   (e.g., "Add endpoint to revoke authentication tokens" instead of "Add RevokeTokenCommand handler
   with PostgreSQL adapter").
4. Read `src/Directory.Build.props` for the current version; compute the next version using:
   MAJOR for breaking API changes, MINOR for new features or endpoints, PATCH for bug fixes.
5. Ask the user: (a) bump to the computed version, or (b) append to the latest existing version
   in `CHANGELOG.md`; retry once if unclear — if still no answer, return `FAIL` with
   `reason: "no confirmed version choice"`; otherwise proceed to step 6 with the confirmed choice.
6. Apply the confirmed choice:
   - If (a): update `<Version>` in `src/Directory.Build.props` and insert a new
     `## [version] - <today>` section above the latest release in `CHANGELOG.md`.
   - If (b): leave `src/Directory.Build.props` unchanged and merge entries into the existing
     latest version section in `CHANGELOG.md`.
7. Return the structured output with mode, version, and the changelog lines added.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  mode: [mode: enum(CREATE, APPEND)]
  version: [target_version: string]
  previous_version: [previous_version: string]
  bump_type: [bump_type: enum(MAJOR, MINOR, PATCH)]
  date: [iso_date: string]
  entries_added: [entry_count: integer]
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
