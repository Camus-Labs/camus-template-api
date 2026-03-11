---
description: 'Review an agent file and return a verdict based on best practices'
argument-hint: 'Provide the target agent path to review'
mode: 'agent'
tools: ['codebase']
---
# Review Agent Prompt

## Goal

Produce a structured review report for a given `*.agent.md` file by evaluating it against the conventions checklist
from the Context section.

**Success:** Report covers every section and follows the exact Output Format template.

**Failure:** `target_agent_path` is missing, unreadable, or doesn't end with `.agent.md`.

## Context

Read and internalize the conventions checklist before starting:

- #file:.github/instructions/agents.instructions.md

## Inputs

- `target_agent_path` (required, string): workspace-relative path to the target `*.agent.md` file.

## Process

1. Confirm the user supplied `target_agent_path`, points to a readable file, and ends with `.agent.md` using the
  `codebase` tool — if missing, unreadable, or invalid type, stop and report the problem; otherwise, proceed to step 2.
2. Iterate through sections in this exact order using the `codebase` tool: `Writing Quality and Structure`,
  `Frontmatter`, `Role`, `Goal`, `Context`, `Inputs`, `Process`, `Output Format`, `Rules`; evaluate exactly one section
  per iteration, score `PASS` only when ALL items in that section pass — otherwise score FAIL — and record each failing
  item as a finding; stop after 9 iterations.
3. Compute the overall verdict — PASS when every section is PASS, otherwise FAIL — use no tools.
4. Produce the report in the exact output format below using the verdict and all findings — use no tools.

## Rules

- MUST provide evidence in the exact structure for every finding
- MUST include a concrete fix per finding — not generic advice
- MUST NOT rewrite the agent file unless the user explicitly requests it
- MUST NOT invent conventions — validate only against this checklist
- MUST NOT evaluate correctness of the agent’s domain logic

## Output Format

````markdown
## Agent Review Report

**Target:** [target_agent_path]
**Model:** [self-reported model name and version]
**Verdict:** [PASS | FAIL]

### Section Results

| # | Section | Result | Findings |
|---|---------|--------|----------|
| 0 | Writing Quality and Structure | [PASS | FAIL] | [count] |
| 1 | Frontmatter | [PASS | FAIL] | [count] |
| 2 | Role | [PASS | FAIL] | [count] |
| 3 | Goal | [PASS | FAIL] | [count] |
| 4 | Context | [PASS | FAIL] | [count] |
| 5 | Inputs | [PASS | FAIL] | [count] |
| 6 | Process | [PASS | FAIL] | [count] |
| 7 | Output Format | [PASS | FAIL] | [count] |
| 8 | Rules | [PASS | FAIL] | [count] |

### Findings

Section [#] — [issue]
- Evidence:
  Heading: [exact heading text] | Quote: "[exact source text]"
  Location: [Section:heading path OR Line: Lx-Ly]
- Fix: [corrective action]
````
