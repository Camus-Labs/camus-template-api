---
description: Review a prompt file and return verdict based on best practices
argument-hint: Outline the target prompt path to review
mode: 'agent'
tools: ['codebase']
---
# Review Prompt File

## Goal

Produce a structured review report for a given `*.prompt.md` file by evaluating
it against the conventions checklist referenced in the Context section. The report
must include a `PASS` or `FAIL` verdict per section, a finding per failed item
with evidence and a concrete fix, and an overall `Ready for Use` determination.

**Success:** All sections are evaluated and the report is produced in the exact
Output Format template.
**Failure:** `target_prompt_path` is missing, unreadable, or does not end with
`.prompt.md` — stop and report the problem.

## Context

Read and internalize the conventions checklist before starting:

- #file:.github/instructions/prompts.instructions.md

## Inputs

- `target_prompt_path` (required, string): workspace-relative path to the target
  `*.prompt.md` file.

## Process

1. Validate `target_prompt_path` is provided, points to a readable file, and ends
   with `.prompt.md` using the `codebase` tool — if missing, unreadable, or
   invalid type, stop and report the problem.
2. Evaluate structural sections (1–2: Frontmatter, Goal) using the `codebase`
   tool — score `PASS` only when ALL items in a section pass; record each failing
   item as a finding.
3. Evaluate content sections (3–6: Context, Inputs, Process, Output Format) using
   the `codebase` tool — score `PASS` only when ALL items in a section pass;
   record each failing item as a finding.
4. Evaluate behavioral sections (7–8: Rules, Structural Consistency) using the
   `codebase` tool — score `PASS` only when ALL items in a section pass; record
   each failing item as a finding.
5. Evaluate writing quality (section 9) using the `codebase` tool — score `PASS`
   only when ALL items pass; record each failing item as a finding.
6. Compute the overall verdict and produce the report in the exact output format
   below — no tools required.

## Rules

- MUST provide evidence in the exact structure for every finding
- MUST include a concrete fix per finding — not generic advice
- MUST NOT rewrite the prompt file unless explicitly requested
- MUST NOT invent conventions — validate only against this checklist
- MUST NOT evaluate correctness of the prompt's domain logic
- MUST use the reference prompt as the quality baseline for ambiguous cases

## Output Format

```markdown
## Prompt Review Report

**Target:** [target_prompt_path]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Result | Findings |
|---|---------|--------|----------|
| 1 | Frontmatter | [PASS | FAIL] | [count] |
| 2 | Goal | [PASS | FAIL] | [count] |
| 3 | Context | [PASS | FAIL] | [count] |
| 4 | Inputs | [PASS | FAIL] | [count] |
| 5 | Process | [PASS | FAIL] | [count] |
| 6 | Output Format | [PASS | FAIL] | [count] |
| 7 | Rules | [PASS | FAIL] | [count] |
| 8 | Structural Consistency | [PASS | FAIL] | [count] |
| 9 | Writing Quality | [PASS | FAIL] | [count] |

### Findings

Section [#] — [issue]
- Evidence: Heading: [exact heading text] | Quote: "[exact source text]" | Location: [Section: <heading path> OR Line: Lx-Ly]
- Fix: [corrective action]

### Summary

- Ready for Use: [Yes / No]
```
