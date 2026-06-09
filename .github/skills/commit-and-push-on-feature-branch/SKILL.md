---
name: commit-and-push-on-feature-branch
description: 'Stage every working-tree change and, in two phases, prepare a Conventional Commit preview then commit and push to the active feature branch to deliver verified worker edits once the caller supplies approved=true.'
argument-hint: 'Provide feature_slug, commit_type (feat|fix|test|chore|docs), commit_subject, and optional approved (default false)'
user-invocable: false
---

# Commit And Push On Feature Branch

## When to Use

- Persist a worker agent's story edits (architect, tester.unit, developer, tester.integration) to the
  `feat/<slug>` branch under the standard Conventional Commit format.
- Enforce a single commit-message convention across every worker agent without duplicating the `git add && git
  commit && git push` triplet inside each agent file.

## Procedure

1. Validate inputs — return `FAIL` with `reason: "feature_slug missing or invalid"` if `feature_slug` does
   not match `^[a-z0-9]+(-[a-z0-9]+)*$`; return `FAIL` with `reason: "commit_type invalid"` if `commit_type`
   is not one of `feat`, `fix`, `test`, `chore`, `docs`; return `FAIL` with `reason: "commit_subject
   missing"` if `commit_subject` is empty; treat `approved` as `false` when omitted.
2. Verify branch position — run `git rev-parse --abbrev-ref HEAD` and capture the result as
   `current_branch`; if `current_branch` does not equal `feat/$feature_slug`, return `FAIL` with `reason:
   "not on feat/$feature_slug — invoke ensure-on-feature-branch first"` and `details: $current_branch`;
   otherwise set `feature_branch="feat/$feature_slug"` and
   `commit_message="$commit_type($feature_slug): $commit_subject"` and proceed.
3. Stage every working-tree change — run `git add -A`; on failure, return `FAIL` with `reason: "git add
   failed"` and the git error; otherwise proceed.
4. Detect empty stage — run `git diff --cached --quiet`; if the command exits zero (no staged changes),
   return `PARTIAL` with `feature_slug`, `feature_branch`, and `reason: "no changes to commit"`; otherwise
   capture `change_summary` from `git diff --cached --stat` and proceed.
5. Branch on approval — if `approved` is not `true`, return `PARTIAL` with `feature_slug`, `feature_branch`,
   `commit_message`, `change_summary`, and `reason: "approval required — re-invoke with approved=true"` so
   the caller can present the preview to the user and decide; if `approved` is `true`, proceed.
6. Commit the staged changes — run `git commit -m "$commit_message"`; on failure, return `FAIL` with
   `reason: "git commit failed"` and the git error; otherwise capture `commit_sha` via
   `git rev-parse HEAD` and proceed.
7. Push to origin — run `git push origin "$feature_branch"`; on failure, return `FAIL` with `reason: "git
   push failed"` and the git error; otherwise return `SUCCESS` with `feature_slug`, `feature_branch`,
   `commit_sha`, and `commit_message`.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  feature_slug: [feature_slug: string]
  feature_branch: [feature_branch: string]
  commit_sha: [commit_sha: string]
  commit_message: [commit_message: string]
```

```yaml
PARTIAL:
  feature_slug: [feature_slug: string]
  feature_branch: [feature_branch: string]
  reason: [reason: string]
  commit_message: [commit_message_or_absent: string]
  change_summary: [change_summary_or_absent: string]
```

```yaml
FAIL:
  feature_slug: [feature_slug_or_absent: string]
  reason: [reason: string]
  details: [diagnostic_output: string]
```

## Dependencies

- `git` — version-control CLI used to stage, commit, and push worker edits.
