---
description: 'Discover documentation for modified files and produce a coherence and convention compliance review report'
argument-hint: 'Provide the list of modified files whose documentation impact to review'
mode: 'agent'
tools: ['search']
---

# Review Documentation

## Goal

Discover all documentation files related to the provided modified files and produce a structured review report by
verifying content coherence and evaluating compliance with the documentation conventions checklist.

**Success:** Report covers the content coherence check and every conventions checklist section, and follows the exact
Output Format template.

**Failure:** Any input validation in Steps 1–2 fails, Step 3 discovers zero documentation files, or Step 4 encounters
an unreadable documentation file.

## Context

Read and internalize the conventions checklist before starting:

- #file:.github/instructions/documentation.instructions.md

## Inputs

- `modified_files` (required, string[]): workspace-relative paths to files that changed (`.cs`, `.json`,
  `.csproj`, `.yml`, or any other type) and serve as ground truth for verifying documentation accuracy.

## Process

1. Confirm the caller supplied at least one `modified_files` entry — if the list is empty, stop and report the problem;
  otherwise proceed to Step 2.

2. Read every file in `modified_files` — understand public types, interfaces, configuration keys, settings structures,
  endpoints, and behavioral contracts; if any file is unreadable, stop and report the problem; otherwise proceed to
  Step 3.

3. Discover documentation files — for each modified file (up to 50), locate its containing project directory (the folder
  with the `.csproj` file) and collect the `README.md` in that directory if it exists; otherwise skip that project
  directory; then collect `README.md` and `docs/README.md` in the workspace root as baseline files, plus every other
  `docs/*.md` file that references types, configuration keys, settings, or features found in Step 2; deduplicate the
  resulting list; if the search finds zero documentation files, stop and report the problem; otherwise proceed to
  Step 4.

4. Read every discovered documentation file — if any file is unreadable, stop and report the problem; otherwise proceed
  to Step 5.

5. Evaluate content coherence — for each documentation file (up to 25), verify every technical claim, configuration
  reference, type name, endpoint description, and behavioral statement against the source code from Step 2; record a
  finding when documentation describes something that contradicts, misrepresents, or no longer matches the source code;
  score `PASS` when no documentation file contains stale or inaccurate content relative to the source code,
  otherwise score `FAIL`; proceed to Step 6.

6. Evaluate each documentation conventions checklist section (Information Ownership, Single Source of Truth,
  Cross-Reference Integrity, Content Accuracy, Structure & Formatting) against the documentation files — score `PASS`
  only when ALL checklist items in that section pass for ALL documentation files, otherwise score `FAIL`, and record
  each failing item as a finding; proceed to Step 7.

7. Compute the overall verdict — PASS when every section including Content Coherence is PASS, otherwise FAIL.

8. Produce the report in the exact output format below using the verdict and all findings.

## Rules

- MUST provide evidence in the exact structure for every finding, including the file path
- MUST include a concrete fix per finding — not generic advice
- MUST NOT modify any documentation file
- MUST NOT invent conventions — validate only against the documentation conventions checklist
- MUST NOT evaluate correctness of business or domain logic in modified files
- MUST NOT flag missing documentation for code elements that no prior documentation covered — flag only stale or
  inaccurate existing documentation
- MUST use these exact section names: "Content Coherence" followed by the conventions checklist section names
- MUST NOT discover files in `.github/`, test projects, or infrastructure directories

## Output Format

````markdown
## Documentation Review Report

**Modified Files:** [count] files
**Discovered Documentation Files:** [list of workspace-relative paths]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Result | Findings |
|---|---------|--------|----------|
| 1 | Content Coherence | [PASS | FAIL] | [count] |
| 2 | Information Ownership | [PASS | FAIL] | [count] |
| 3 | Single Source of Truth | [PASS | FAIL] | [count] |
| 4 | Cross-Reference Integrity | [PASS | FAIL] | [count] |
| 5 | Content Accuracy | [PASS | FAIL] | [count] |
| 6 | Structure & Formatting | [PASS | FAIL] | [count] |

### Findings

Section [#] — [file path] — [issue]
- Evidence:
  File: [workspace-relative file path] | Quote: "[exact source text or missing reference]"
  Location: [Section:heading path OR Line: Lx-Ly]
- Fix: [corrective action]

### Summary

- Total Sections: [count]
- Total Findings: [count]
- Files Reviewed: [count]
````
