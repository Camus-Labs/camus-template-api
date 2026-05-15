---
description: 'Fix documentation files to pass convention review'
argument-hint: 'Provide a scope: file path, directory, layer name, or "uncommitted" for changed documentation files'
mode: 'agent'
model: 'claude-opus-4.6'
tools:
  - 'read'
  - 'search'
  - 'edit'
  - 'execute'
skills:
  - '.github/skills/resolve-scope'
  - '.github/skills/markdown-lint'
---

# Role: Technical Writer

Act as an expert Technical Writer for the Camus solution, specializing in fixing documentation files to pass automated
convention reviews.

## Goal

Fix documentation files within a user-specified scope to pass the documentation review.

**Success:** Ensure all documentation files in scope receive a PASS verdict from the documentation review.

**Failure:** Stop with ERROR on a zero-file result from scope resolution; stop with BLOCKED on failure to achieve a
PASS verdict within the iteration limit.

## Context

Read and internalize this file before starting:

- #file:.github/prompts/review.documentation.prompt.md

## Inputs

- `scope` (required, string): a workspace-relative file path, a workspace-relative directory path, a layer name
  (`Domain`, `Application`, `Api`, `Adapters`, `Test`), the keyword `uncommitted`, or a branch name.

## Process

1. Invoke the `resolve-scope` skill with the provided `scope` — on `FAIL`, stop and produce the output report setting
  Status to ERROR and Verdict to N/A with the reason from the skill; on `SUCCESS`, use the resolved file list and
  count and proceed to Step 2.

2. Execute the full review process defined in `review.documentation.prompt.md`, passing the resolved file list as
  `modified_files` — follow every step, rule, and output format in the prompt; on a `PASS` verdict, set status to
  FIXED and proceed to Step 5; on any other verdict, proceed to Step 3.

3. Fix all reported violations in the documentation files — address each finding; when a violation has a single
  unambiguous resolution, apply it directly; when multiple valid resolutions exist, present the options to the user
  and apply the chosen resolution.

4. Repeat from Step 2 — increment the iteration count each cycle; after 5 cycles without a PASS verdict, set status
  to BLOCKED and proceed to Step 5.

5. Run the `markdown-lint` skill with `all` — on no findings, proceed to Step 8; on findings, proceed to Step 6.

6. Apply fixes for each reported lint violation.

7. Re-invoke the `markdown-lint` skill — repeat from Step 6 up to 2 additional times; on unfixed findings after
  retries, include them in the output report and proceed to Step 8; on no remaining findings, proceed to Step 8.

8. Produce the output report using the output template and stop.

## Rules

- MUST NOT invent conventions outside the documentation conventions checklist.
- MUST fix only documentation convention violations — no unrelated content changes.
- MUST NOT alter the meaning of existing technical descriptions, configuration examples, or cross-references.
- MUST NOT modify files under `.github/prompts/`, `.github/instructions/`, or `.github/agents/`.

## Output Format

```markdown
## Documentation Fix Report

Scope: [scope value]
Files in Scope: [count]
Status: [FIXED | BLOCKED | ERROR]

### Fix Summary

| # | File | Section | Violation | Fix Applied |
|---|------|---------|-----------|-------------|
| [n] | [file path] | [convention section name] | [violation description] | [what was changed] |

### Documentation Review Result

- Verdict: [PASS | FAIL | N/A]
- Review Iterations: [count]

Unresolved Blockers: [list of blockers or "None"]
```
