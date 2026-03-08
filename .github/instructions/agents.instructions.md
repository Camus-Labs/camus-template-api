---
applyTo: ".github/agents/**"
---

# Agents Development Conventions

1. Writing Quality and Structure

    - [ ] Sections in canonical order: Frontmatter → Role → Goal → Context → Inputs → Process → Rules → Output Format
    - [ ] No extra top-level sections outside the canonical set
    - [ ] Heading hierarchy is correct (H1 title, H2 sections, NO H3+ subsections)
    - [ ] No line exceeds 120 characters
    - [ ] No under-utilized line width — unfinished prose lines must break near 120 chars
    - [ ] No redundant sections
    - [ ] Active voice, imperative mood
    - [ ] No orphan outputs — every process step output feeds a downstream step or the final output template
    - [ ] No cross-section contradictions — no section makes a claim that another section negates

2. Frontmatter

    - [ ] Valid YAML `---` frontmatter present
    - [ ] `description` — one sentence: verb + object + outcome
    - [ ] `mode` — defines the best mode to achieve the desired goal
    - [ ] `tools` — lists ONLY tools the process steps actually use
    - [ ] `argument-hint` — is included describing how to use the prompt
    - [ ] No over-privileged tools (listed but never used in steps)
    - [ ] No under-declared tools (used in steps but not listed)

3. Role

    - [ ] Role section exists (H1 `# Role: {Name}`)
    - [ ] Opening paragraph states persona, expertise, and single deliverable
    - [ ] Scoped to ONE responsibility — no mixed roles
    - [ ] No verbs that conflict with the role (reviewer that "fixes")

4. Goal

    - [ ] Goal section exists (H2 `## Goal`)
    - [ ] Concrete outcome stated (report, file, fix, plan)
    - [ ] Success criteria are binary-testable
    - [ ] Failure conditions are explicit

5. Context

    - [ ] Exists if additional context is required
    - [ ] Lists required files using `#file:` references
    - [ ] Minimal — only what the process steps need
    - [ ] No stale references (every referenced file exists and is relevant)

6. Inputs

    - [ ] Inputs section exists (H2 `## Inputs`)
    - [ ] Required inputs listed with format/type
    - [ ] Optional inputs marked with defaults
    - [ ] No dead inputs (listed but never consumed)
    - [ ] No phantom inputs (consumed in process but never declared)

7. Process

    - [ ] Process section exists (H2 `## Process`)
    - [ ] Steps numbered.
    - [ ] Logical dependency order (later steps use earlier outputs)
    - [ ] Each step starts with ONE action verb
    - [ ] 3–8 total steps
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
    - [ ] Conditionals have explicit ELSE or default
    - [ ] Loops have max-iteration bound
    - [ ] Explicit stopping criterion
    - [ ] First step validates inputs; last step produces the output
    - [ ] One bounded action per step — sub-item enumeration within one target is fine; no independent evaluations
    - [ ] Steps that need tools name them explicitly
    - [ ] No step contradicts another step or a rule

8. Rules

    - [ ] Rules section exists (H2 `## Rules`)
    - [ ] Each rule is one imperative sentence (MUST X / MUST NOT Y)
    - [ ] Rules are falsifiable
    - [ ] At least one scope-limiting rule
    - [ ] No rule contradicts or duplicates a process step
    - [ ] No rule is unenforceable or purely aspirational

9. Output Format

    - [ ] Output Format section exists (H2 `## Output Format`)
    - [ ] Fenced code block with COMPLETE report template
    - [ ] Placeholder syntax consistent (`[value]` throughout)
    - [ ] Verdict/status labels enumerated (`PASS | FAIL`, not "a status")
    - [ ] Template is copy-pasteable — no prose inside
    - [ ] Every process-computed value has a placeholder in the template
    - [ ] Sufficient for downstream handoff without re-reading source
