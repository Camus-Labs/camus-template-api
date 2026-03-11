---
applyTo: ".github/agents/**"
---

# Agents Development Conventions

1. Writing Quality and Structure

    - [ ] Sections appear in canonical order: Frontmatter → Role → Goal → Context → Inputs → Process → Rules → Output Format
    - [ ] No extra top-level sections outside the canonical set
    - [ ] Heading hierarchy is correct (H1 title, H2 sections, NO H3+ subsections)
    - [ ] No line exceeds 120 characters — markdown table rows are exempt
    - [ ] Prose lines that are not the last line of a paragraph contain at least 100 characters
    - [ ] No two sections contain the same constraint, instruction, or definition
    - [ ] All prose uses active voice, imperative mood — no passive constructions ("is generated", "should be done")
    - [ ] Every process step output is referenced in a later step or in the output template

2. Frontmatter

    - [ ] Valid YAML `---` frontmatter present
    - [ ] `description` — one sentence: verb + object + outcome
    - [ ] `mode` — one of `agent` | `ask` | `edit`
    - [ ] `argument-hint` — included, describes how to invoke the prompt
    - [ ] No over-privileged tools (listed but never used in steps)
    - [ ] No under-declared tools (used in steps but not listed)

3. Role

    - [ ] Role section exists (H1 `# Role: {Name}`)
    - [ ] Opening paragraph states persona and expertise
    - [ ] Scoped to ONE responsibility — no mixed roles
    - [ ] No verbs that conflict with the role (reviewer that "fixes")

4. Goal

    - [ ] Goal section exists (H2 `## Goal`)
    - [ ] Outcome names a concrete artifact type — report, file, fix, or plan
    - [ ] Success criteria specify pass/fail conditions
    - [ ] Goal lists failure conditions
    - [ ] Goal describes exactly one deliverable

5. Context

    - [ ] Context section omitted only when no process step references a `#file:` or external data source
    - [ ] Lists required files using `#file:` references
    - [ ] Every listed context item is consumed by at least one process step
    - [ ] Every `#file:` reference targets an existing workspace file

6. Inputs

    - [ ] Inputs section exists (H2 `## Inputs`)
    - [ ] Required inputs include name, format, and type
    - [ ] Optional inputs carry explicit defaults
    - [ ] No dead inputs (listed but never consumed in process or output)
    - [ ] No phantom inputs (consumed in process but never declared)

7. Process

    - [ ] Process section exists (H2 `## Process`)
    - [ ] Steps are numbered
    - [ ] Logical dependency order (later steps use earlier outputs)
    - [ ] Each step starts with ONE action verb
    - [ ] 3–8 total steps
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
    - [ ] Conditionals have explicit ELSE or default
    - [ ] Loops have max-iteration bound
    - [ ] Explicit stopping criterion
    - [ ] First step validates inputs
    - [ ] Last step produces the output
    - [ ] One bounded action per step — sub-item enumeration within one target is fine; no independent evaluations
    - [ ] Steps that invoke tools name them explicitly
    - [ ] No step restates, negates, or overrides another step or rule
    - [ ] Every process-computed value has a placeholder in the template

8. Rules

    - [ ] Rules section exists (H2 `## Rules`)
    - [ ] Each rule is one imperative sentence (MUST X / MUST NOT Y)
    - [ ] Rules are falsifiable (can be verified true or false)
    - [ ] At least one scope-limiting rule
    - [ ] No rule restates, negates, or overrides another rule
    - [ ] No rule is unenforceable or purely aspirational

9. Output Format

    - [ ] Output Format section exists (H2 `## Output Format`)
    - [ ] Fenced code block with COMPLETE report template
    - [ ] Placeholder syntax consistent (`[value]` throughout)
    - [ ] Verdict/status labels enumerated (`PASS | FAIL`, not "a status")
    - [ ] Template is copy-pasteable — no prose inside the fence
