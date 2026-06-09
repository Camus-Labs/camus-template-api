---
name: commit-and-push-on-release-branch
description: 'Stage every working-tree change and, in two phases, prepare a Conventional Commit preview then commit and push to the active release branch to persist release-scope edits under the standard Conventional Commit format.'
argument-hint: 'Provide commit_type (feat|fix|test|chore|docs), commit_scope, commit_subject, and optional approved (default false)'
user-invocable: false
---

# Commit And Push On Release Branch

## When to Use

- Persist a release-scope agent's edits (product_owner, tester.qa, technical_writer, release_manager) to the
  active `release/<version>` branch under the standard Conventional Commit format.
- Commit and push when any release-scope agent has completed its edits and must record them on the active
  `release/<version>` branch with a validated Conventional Commit message.

## Procedure

1. Validate inputs — return `FAIL` with `reason: "commit_type invalid"` if `commit_type` is not one of `feat`,
   `fix`, `test`, `chore`, `docs`; return `FAIL` with `reason: "commit_scope missing or invalid"` if
   `commit_scope` does not match `^[a-zA-Z0-9._-]+$`; return `FAIL` with `reason: "commit_subject missing"` if
   `commit_subject` is empty; treat `approved` as `false` when omitted.
2. Verify branch position — run `git rev-parse --abbrev-ref HEAD` and capture the result as `current_branch`;
   if `current_branch` does not match `^release/` , return `FAIL` with `reason: "not on a release branch —
   invoke ensure-on-release-branch first"` and `details: $current_branch`; otherwise set
   `release_branch="$current_branch"` and
   `commit_message="$commit_type($commit_scope): $commit_subject"` and proceed.
3. Stage every working-tree change — run `git add -A`; on failure, return `FAIL` with `reason: "git add
   failed"` and the git error; otherwise proceed.
4. Detect empty stage — run `git diff --cached --quiet`; if the command exits zero (no staged changes), return
   `PARTIAL` with `release_branch` and `reason: "no changes to commit"`; otherwise capture `change_summary`
   from `git diff --cached --stat` and proceed.
5. Branch on approval — if `approved` is not `true`, return `PARTIAL` with `release_branch`, `commit_message`,
   `change_summary`, and `reason: "approval required — re-invoke with approved=true"`; if `approved` is
   `true`, proceed.
6. Commit the staged changes — run `git commit -m "$commit_message"`; on failure, return `FAIL` with
   `reason: "git commit failed"` and the git error; otherwise capture `commit_sha` via
   `git rev-parse HEAD` and proceed.
7. Push to origin — run `git push origin "$release_branch"`; on failure, return `FAIL` with `reason: "git push
   failed"` and the git error; otherwise return `SUCCESS` with `release_branch`, `commit_sha`, and
   `commit_message`.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  release_branch: [release_branch: string]
  commit_sha: [commit_sha: string]
  commit_message: [commit_message: string]
```

```yaml
PARTIAL:
  release_branch: [release_branch: string]
  reason: [reason: string]
  commit_message: [commit_message_or_absent: string]
  change_summary: [change_summary_or_absent: string]
```

```yaml
FAIL:
  release_branch: [release_branch_or_absent: string]
  reason: [reason: string]
  details: [diagnostic_output_or_absent: string]
```

## Dependencies

- `git` — version-control CLI; stage, commit, and push release-scope edits.
- `ensure-on-release-branch` — sibling skill; verify checkout position before commit.
