---
description: 'Review a prompt file and return verdict based on best practices'
argument-hint: 'Outline the target prompt path to review'
mode: 'agent'
tools: ['codebase']
---
# Review Prompt File

## Goal

Produce a structured review report for a given `*.prompt.md` file by evaluating it against the conventions checklist
referenced in the Context section. Generate the report following the specified Process and Output Format.

**Success:** Report covers every section and follows the exact Output Format template.

**Failure:** `target_prompt_path` is missing, unreadable, or doesn't end with `.prompt.md` — stop and report the
problem.

## Context

Read and internalize the conventions checklist before starting:

- #file:.github/instructions/prompts.instructions.md

## Inputs

- `target_prompt_path` (required, string): workspace-relative path to the target `*.prompt.md` file.

## Process

1. Validate `target_prompt_path` is provided, points to a readable file, and ends with `.prompt.md` using the `codebase`
  tool — if missing, unreadable, or invalid type, stop and report the problem; otherwise, proceed to step 2.
2. Iterate through sections in this exact order using the `codebase` tool: `Writing Quality and Structure`,
  `Frontmatter`, `Goal`, `Context`, `Inputs`, `Process`, `Output Format`, `Rules`; evaluate exactly one section per
  iteration, score `PASS` only when ALL items in that section pass — otherwise score FAIL - and record each failing
  item as a finding; stop after 8 iterations.
3. Compute the overall verdict — PASS when every section is PASS, otherwise FAIL — no tools required.
4. Produce the report in the exact output format below using the verdict and all findings — no tools required.

## Rules

- MUST provide evidence in the exact structure for every finding
- MUST include a concrete fix per finding — not generic advice
- MUST NOT rewrite the prompt file unless explicitly requested
- MUST NOT invent conventions — validate only against this checklist
- MUST NOT evaluate correctness of the prompt's domain logic

## Output Format

```markdown
## Prompt Review Report

**Target:** [target_prompt_path]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Result | Findings |
|---|---------|--------|----------|
| 0 | Writing Quality and Structure | [PASS | FAIL] | [count] |
| 1 | Frontmatter | [PASS | FAIL] | [count] |
| 2 | Goal | [PASS | FAIL] | [count] |
| 3 | Context | [PASS | FAIL] | [count] |
| 4 | Inputs | [PASS | FAIL] | [count] |
| 5 | Process | [PASS | FAIL] | [count] |
| 6 | Output Format | [PASS | FAIL] | [count] |
| 7 | Rules | [PASS | FAIL] | [count] |

### Findings

Section [#] — [issue]
- Evidence:
  Heading: [exact heading text] | Quote: "[exact source text]"
  Location: [Section:heading path OR Line: Lx-Ly]
- Fix: [corrective action]
```
