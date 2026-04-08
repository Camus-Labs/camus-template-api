---
description: 'Review C# source files against their matching instruction checklists and return a verdict'
argument-hint: 'Provide the list of modified .cs files to review'
mode: 'agent'
tools:
  - 'read'
  - 'search'
---

# Review Code

## Goal

Produce a structured review report for one or more `.cs` files by matching each file to its applicable instruction
checklists and evaluating compliance.

**Success:** Report covers every checklist section from every matched instruction file and follows the exact Output
Format template.

**Failure:** Any input validation in Steps 1–2 fails, or Step 3 matches zero files.

## Context

Read and internalize the instruction checklists before starting:

- #file:.github/instructions/csharp.instructions.md
- #file:.github/instructions/domain.instructions.md
- #file:.github/instructions/application.instructions.md
- #file:.github/instructions/api.instructions.md
- #file:.github/instructions/adapters.instructions.md
- #file:.github/instructions/adapters.persistence.instructions.md
- #file:.github/instructions/testing.instructions.md
- #file:.github/instructions/testing.unit.instructions.md
- #file:.github/instructions/testing.integration.instructions.md

## Inputs

- `modified_files` (required, string[]): workspace-relative paths to the `.cs` files to evaluate.

## Process

1. Confirm the caller supplied at least one `modified_files` entry — if the list is empty, stop and report the problem;
  otherwise proceed to Step 2.

2. Read every file in `modified_files` — if any file is unreadable or is not a `.cs` file, stop and report the problem;
  otherwise proceed to Step 3.

3. Match each file to its applicable instruction checklists by evaluating the `applyTo` glob patterns that each
  instruction file frontmatter declares — apply these rules in order:
    - `src/Test/**` (excluding `*.integration.test`) → `testing.instructions` + `testing.unit.instructions`
    - `src/Test/**integration.test/**` → `testing.instructions` + `testing.integration.instructions`
    - `src/Domain/**/*.cs` → `csharp.instructions` + `domain.instructions`
    - `src/Application/**/*.cs` → `csharp.instructions` + `application.instructions`
    - `src/Api/**/*.cs` → `csharp.instructions` + `api.instructions`
    - `src/Adapters/emc.camus.persistence.postgresql/**/*.cs` → `csharp.instructions` + `adapters.instructions` +
      `adapters.persistence.instructions`
    - `src/Adapters/**/*.cs` (all other adapters) → `csharp.instructions` + `adapters.instructions`
    - If no file matches any pattern, stop and report; otherwise build a combined ordered section list from the matched
      instruction checklists and proceed to Step 4.

4. Iterate through the combined section list (max 30 sections); evaluate exactly one section per iteration across ALL
  matched files, score `PASS` only when ALL checklist items in that section pass for ALL applicable files — otherwise
  score `FAIL` — and record each failing item as a finding; stop after evaluating all sections.

5. Compute the overall verdict — PASS when every section is PASS, otherwise FAIL.

6. Produce the report in the exact output format below using the verdict and all findings.

## Rules

- MUST provide evidence in the exact structure for every finding, including the file path
- MUST include a concrete fix per finding — not generic advice
- MUST NOT modify any target file
- MUST NOT invent conventions — validate only against the supplied instruction checklists
- MUST NOT evaluate correctness of business or domain logic
- MUST use section names exactly as they appear in the instruction files

## Output Format

````markdown
## Code Review Report

**Target Files:** [count] files
**Matched Instructions:** [comma-separated instruction file names]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Source Instruction | Result | Findings |
|---|---------|-------------------|--------|----------|
| [n] | [section name from instruction] | [instruction file name] | [PASS | FAIL] | [count] |

### Findings

Section [#] — [file path] — [issue]
- Evidence:
  File: [workspace-relative file path] | Quote: "[exact source text]"
  Location: [Class:member OR Line: Lx-Ly]
- Fix: [corrective action]

### Summary

- Total Sections: [count]
- Total Findings: [count]
- Files Reviewed: [count]
````
