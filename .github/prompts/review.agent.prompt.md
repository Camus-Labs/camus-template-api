---
description: Review an agent file and return verdict based on best practices
argument-hint: Outline the target agent path to review
mode: 'agent'
tools: ['codebase']
---
# Review Agent Prompt

## Goal

Produce a structured review report for a given `*.agent.md` file by evaluating it against the conventions checklist
referenced in the Context section. The report must include a `PASS` or `FAIL` verdict per section, a finding per failed
item with evidence and a concrete fix, and an overall `Ready for Use` determination.

**Success:** All sections are evaluated and the report is produced in the exact Output Format template.

**Failure:** `target_agent_path` is missing, unreadable, or does not end with `.agent.md` — stop and report the problem.

## Context

Read and internalize the conventions checklist before starting:

- #file:.github/instructions/agents.instructions.md

## Inputs

- `target_agent_path` (required, string): workspace-relative path to the target `*.agent.md` file.

## Process

1. Validate `target_agent_path` is provided, points to a readable file, and ends with `.agent.md` using the `codebase`
   tool — if missing, unreadable, or invalid type, stop and report the problem.
2. Evaluate structural sections (1–3: Frontmatter, Role, Goal) using the `codebase` tool — score `PASS` only when ALL
   items in a section pass; record each failing item as a finding.
3. Evaluate content sections (4–7: Context, Inputs, Process, Output Format) using the `codebase` tool — score `PASS`
   only when ALL items in a section pass; record each failing item as a finding.
4. Evaluate behavioral sections (8–9: Rules, Writing Quality) using the `codebase` tool — score `PASS` only when ALL
   items in a section pass; record each failing item as a finding.
5. Compute the overall verdict and produce the report in the exact output format below — no tools required.

## Rules

- MUST provide evidence in this exact structure for every finding
- MUST include a concrete fix per finding — not generic advice
- MUST NOT rewrite the agent file unless explicitly requested
- MUST NOT invent conventions — validate only against this checklist
- MUST NOT evaluate correctness of the agent’s domain logic

## Output Format

```markdown
## Agent Review Report

**Target:** [target_agent_path]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Result | Findings |
|---|---------|--------|----------|
| 1 | Frontmatter | [PASS | FAIL] | [count] |
| 2 | Role | [PASS | FAIL] | [count] |
| 3 | Goal | [PASS | FAIL] | [count] |
| 4 | Context | [PASS | FAIL] | [count] |
| 5 | Inputs | [PASS | FAIL] | [count] |
| 6 | Process | [PASS | FAIL] | [count] |
| 7 | Output Format | [PASS | FAIL] | [count] |
| 8 | Rules | [PASS | FAIL] | [count] |
| 9 | Writing Quality | [PASS | FAIL] | [count] |

### Findings

Section [#] — [issue]
- Evidence:
  Heading: [exact heading text] | Quote: "[exact source text]"
  Location: [Section: <heading path> OR Line: Lx-Ly]
- Fix: [corrective action]

### Summary

- Ready for Use: [Yes / No]
```
