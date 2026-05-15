---
name: resolve-scope
description: 'Resolve a scope argument (file path, directory, layer name, "uncommitted", or branch) to a sorted, workspace-relative list of .cs files for downstream review or analysis.'
argument-hint: 'Provide a scope: file path, directory path, layer name (Domain, Application, Api, Adapters, Test), "uncommitted", or a branch name'
user-invocable: false
---

# Resolve Scope

## When to Use

- Use before dispatching code review sub-agents that require a resolved file list.
- Use when a scope value must map to concrete `.cs` file paths.
- Use when filtering uncommitted changes to `.cs` files only.

## Procedure

1. Validate that the `scope` argument is present and non-empty — if missing, return `FAIL` with reason
   `"scope argument is missing"`; otherwise proceed to step 2.
2. Resolve `scope` by classifying it into one of six categories:
    - **File path**: confirm the file exists and ends with `.cs` — if yes, produce a single-item list; else produce
      an empty list.
    - **Directory path**: run `find "<workspace-root>/<scope>" -type f -name "*.cs"` and collect the output lines as
      the list.
    - **Layer name** (`Domain`, `Application`, `Api`, `Adapters`, `Test`): map to `src/<layer>/` and run
      `find "<workspace-root>/src/<layer>" -type f -name "*.cs"` to produce the list.
    - **`uncommitted`**: run `git diff --name-only HEAD` combined with `git ls-files --others --exclude-standard`
      and filter output to lines ending in `.cs` only.
    - **Branch name**: run `git rev-parse --verify "<scope>"` to confirm it is a valid ref — if valid, run
      `git diff --name-only main..."<scope>"` and filter output to lines ending in `.cs` only; if the ref does not
      exist, produce an empty list.
    - **Unrecognized**: produce an empty list.
3. Convert all paths in the list to workspace-relative form (strip the workspace-root prefix).
4. Return `FAIL` with reason `"no .cs files found for scope: <scope>"` if the list is empty; otherwise proceed
   to step 5.
5. Sort the list alphabetically and return `SUCCESS` with the file count and sorted file list.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  scope: [scope: string]
  count: [file_count: integer]
  files:
    - [file_path: string]
    - [file_path: string]
```

```yaml
FAIL:
  scope: [scope_or_missing: string]
  reason: [reason: string]
```

## Dependencies

- `find` — POSIX file-search utility; locates `.cs` files under a given directory tree.
- `git` — version-control CLI; lists uncommitted changes and compares branches.
