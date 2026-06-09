---
name: complete-feature
description: 'Validate that every story in a feature folder is signed and Done and merge the feature branch into the release/next branch via a squash pull request to deliver a single feature commit on the release.'
argument-hint: 'Provide feature_slug (lowercase kebab-case) and optional approved (default false)'
user-invocable: true
---

# Complete Feature

## When to Use

- Close a feature whose every story is `Done` and whose every Sections A–D handoff gate reads `Yes` or `N/A`.
- Open and merge the `feat/<feature-slug> → release/next` pull request with a squash merge to land a
  single commit on the placeholder release branch.

## Procedure

1. Validate inputs — return `FAIL` with `reason: "feature_slug missing or invalid"` if `feature_slug`
   does not match `^[a-z0-9]+(-[a-z0-9]+)*$`; treat `approved` as `false` when omitted; invoke the
   `ensure-on-feature-branch` skill with `feature_slug`; on `FAIL`, return `FAIL` with the skill's reason;
   otherwise capture `release_version`, `release_branch`, `feature_branch`, and `feature_folder` and proceed.
2. Enumerate every `US-*.md` file in `feature_folder` and confirm each story `Metadata.Status` reads `Done`
   and every Sections A–D handoff gate item (Product Owner, Architect, Tester, Developer, Integration Tester)
   reads `Yes` or `N/A` — collect each failing story with its blocking field; if any story fails, return `FAIL`
   with `reason: "stories not ready"` and the failing list; otherwise proceed.
3. Update `feature_folder/_feature.md` by setting `Metadata.Status` to `Done` and ensuring every row of the
   `Stories` table reads `Done`; on no change, skip to Step 4; otherwise run `git add "$feature_folder/_feature.md"
   && git commit -m "chore($feature_slug): mark feature done" && git push origin "$feature_branch"`; on
   failure, return `FAIL` with `reason: "git commit/push failed"` and the git error; otherwise proceed.
4. Open the feature-to-release pull request by running `gh pr create --base "$release_branch" --head
   "$feature_branch" --title "feat: $feature_slug" --body "Closes feature $feature_slug. Squash merge into
   $release_branch."`; on `gh pr already exists`, capture the existing PR URL via `gh pr view --json url
   --jq .url`; on other failures, return `FAIL` with `reason: "gh pr create failed"` and the error; otherwise
   capture `pr_url` and proceed.
5. Branch on approval — if `approved` is not `true`, return `PARTIAL` with `feature_slug`,
   `release_branch`, `feature_branch`, `pr_url`, and `reason: "approval required — re-invoke with
   approved=true"` so the caller can present the merge preview to the user and decide; if `approved` is
   `true`, proceed.
6. Run `gh pr merge "$pr_url" --squash --delete-branch`; on failure, return `FAIL` with `reason:
   "gh pr merge failed"` and the error; otherwise capture the merge commit SHA via
   `gh pr view "$pr_url" --json mergeCommit --jq .mergeCommit.oid` and proceed.
7. Switch back to the release branch by running `git checkout "$release_branch" && git pull --ff-only`; on
   failure, return `FAIL` with `reason: "post-merge checkout failed"` and the git error; otherwise return
   `SUCCESS` with `feature_slug`, `release_branch`, `feature_branch`, `pr_url`, and `merge_sha`.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  feature_slug: [feature_slug: string]
  release_branch: [release_branch: string]
  feature_branch: [feature_branch: string]
  pr_url: [pr_url: string]
  merge_sha: [merge_sha: string]
```

```yaml
PARTIAL:
  feature_slug: [feature_slug: string]
  release_branch: [release_branch: string]
  feature_branch: [feature_branch: string]
  pr_url: [pr_url: string]
  reason: [reason: string]
```

```yaml
FAIL:
  feature_slug: [feature_slug_or_missing: string]
  reason: [reason: string]
  details: [diagnostic_output_or_failing_list: string]
```

## Dependencies

- `git` — version-control CLI; commit feature status update and pull release branch after merge.
- `gh` — GitHub CLI; create and merge the feature-to-release pull request with squash strategy.
- `ensure-on-feature-branch` — sibling skill; verify the worker is on the feature branch with a clean tree.
