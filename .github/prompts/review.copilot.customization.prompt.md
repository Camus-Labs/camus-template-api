---
description: 'Review copilot customization files against matching convention checklists to produce a compliance verdict'
argument-hint: 'Provide the list of modified customization files to review'
mode: 'agent'
tools:
  - 'read'
  - 'search'
---

# Review Copilot Customization

## Goal

Produce a structured review report for one or more copilot customization files by matching each file to its applicable
convention checklist and evaluating compliance.

**Success:** Deliver a complete, verdict-bearing report with a finding for every non-compliant item.

**Failure:** Report the first validation error and stop without producing a report.

## Context

Read and internalize the convention checklists before starting:

- #file:.github/instructions/agents.instructions.md
- #file:.github/instructions/prompts.instructions.md
- #file:.github/instructions/instructions.instructions.md
- #file:.github/instructions/skills.instructions.md

## Inputs

- `modified_files` (required, string[]): workspace-relative paths to the customization files to evaluate.

## Process

1. Confirm the caller supplied at least one `modified_files` entry — if the list is empty, stop and report the problem;
   otherwise proceed to Step 2.

2. Read every file in `modified_files` (max 20 files) — if any file is unreadable, stop and report the problem;
   otherwise proceed to Step 3.

3. Match each file to its applicable convention checklist by evaluating the filename — apply these rules in order:
    - Ends with `.agent.md` → `agents.instructions` with sections: `Writing Quality and Structure`, `Frontmatter`,
      `Role`, `Goal`, `Context`, `Inputs`, `Process`, `Output Format`, `Rules`
    - Ends with `.prompt.md` → `prompts.instructions` with sections: `Writing Quality and Structure`, `Frontmatter`,
      `Goal`, `Context`, `Inputs`, `Process`, `Output Format`, `Rules`
    - Ends with `.instructions.md` → `instructions.instructions` with sections: `Structure`, `Check Quality`,
      `Scope & Overlap`, `Section Naming`, `Boundary Violations`
    - Filename is `SKILL.md` → `skills.instructions` with sections: `Structure`, `Frontmatter`, `Body Sections`,
      `Procedure Quality`, `Output Contract`, `Self-Containment`, `Writing Quality`
    - If no file matches any pattern, stop and report; otherwise build a combined ordered section list from the matched
      convention checklists and proceed to Step 4.

4. Evaluate each section in the combined section list (max 30 sections) against ALL applicable files — score `PASS`
   when every checklist item passes for every file, otherwise score `FAIL` and record each failing item as a finding.

5. Compute the overall verdict — PASS when every section is PASS, otherwise FAIL.

6. Produce the report in the exact output format below using the verdict and all findings.

## Rules

- MUST provide evidence in the exact structure for every finding, including the file path
- MUST include a concrete fix per finding — not generic advice
- MUST NOT modify any target file
- MUST NOT invent conventions
- MUST validate only against the supplied convention checklists
- MUST NOT evaluate correctness of domain logic
- MUST use section names exactly as they appear in the convention checklist files

## Output Format

````markdown
## Customization Review Report

**Target Files:** [count] files
**Matched Checklists:** [comma-separated checklist file names]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Source Checklist | Result | Findings |
|---|---------|-----------------|--------|----------|
| [n] | [section name from checklist] | [checklist file name] | [PASS | FAIL] | [count] |

### Findings

Section [#] — [file path] — [issue]
- Evidence:
  File: [workspace-relative file path] | Quote: "[exact source text]"
  Location: [Section:heading path OR Line: Lx-Ly]
- Fix: [corrective action]

### Summary

- Total Sections: [count]
- Total Findings: [count]
- Files Reviewed: [count]
````
