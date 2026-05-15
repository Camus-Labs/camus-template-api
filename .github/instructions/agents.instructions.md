---
applyTo: ".github/agents/**/*.agent.md"
---

# Agents Development Conventions

1. Writing Quality and Structure

    - [ ] Sections appear in canonical order: Frontmatter → Role → Goal → Context → Inputs → Process → Rules → Output Format
    - [ ] No extra top-level sections outside the canonical set
    - [ ] Heading hierarchy is correct (H1 title, H2 sections, NO H3+ subsections)
    - [ ] No two sections contain the same constraint, instruction, or definition
    - [ ] All prose uses active voice — no passive constructions ("is generated", "was done")
    - [ ] All prose uses imperative mood — no indicative constructions ("the step generates", "it returns")

2. Frontmatter

    - [ ] Valid YAML `---` frontmatter present
    - [ ] `description` field contains exactly one sentence
    - [ ] `description` sentence follows verb + object + outcome structure
    - [ ] `description` field value is at most 1024 characters
    - [ ] `mode` field present
    - [ ] `mode` field is one of: `agent`, `ask`, `edit`
    - [ ] `argument-hint` field present
    - [ ] `argument-hint` value describes how to invoke the agent
    - [ ] No under-declared tools (used in steps but not listed)

3. Role

    - [ ] Role section exists (H1 `# Role: {Name}`)
    - [ ] Opening paragraph contains a role noun phrase and a named domain qualifier (e.g., "security",
          "data pipeline", "code review")
    - [ ] Opening sentence contains exactly one persona noun phrase — no conjunctions joining distinct roles

4. Goal

    - [ ] Goal section exists (H2 `## Goal`)
    - [ ] Outcome names a concrete artifact type (e.g., report, file, fix, or plan)
    - [ ] `**Success:**` line exists and defines the completion condition as an imperative statement
    - [ ] `**Failure:**` line exists and defines the abort conditions as an imperative statement
    - [ ] Goal section contains exactly one artifact noun — no conjunctions ("and", "or") joining distinct artifact types

5. Context

    - [ ] Context section present only when at least one process step references a `#file:` or external data source
    - [ ] Lists required files using `#file:` references
    - [ ] At least one process step consumes every listed context item
    - [ ] Every `#file:` reference targets an existing workspace file

6. Inputs

    - [ ] Inputs section exists (H2 `## Inputs`)
    - [ ] Required inputs include name, format, and type
    - [ ] Optional inputs carry explicit defaults
    - [ ] No dead inputs (listed but never consumed in process or output)
    - [ ] No phantom inputs (consumed in process but never declared)

7. Process

    - [ ] Process section exists (H2 `## Process`)
    - [ ] Each step carries a sequential number
    - [ ] Each step starts with ONE action verb
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
    - [ ] Conditionals have explicit ELSE or default
    - [ ] Loops have max-iteration bound
    - [ ] First step validates inputs
    - [ ] Last step produces the output
    - [ ] One bounded action per step — sub-item enumeration within one target is fine; no independent evaluations
    - [ ] No two steps share the same leading action verb and direct object
    - [ ] Every process-computed value has a placeholder in the template or is consumed by a subsequent step

8. Rules

    - [ ] Rules section exists (H2 `## Rules`)
    - [ ] Each rule is one imperative sentence (MUST X / MUST NOT Y)
    - [ ] Rules are falsifiable — can be verified true or false
    - [ ] At least one rule begins with "MUST NOT" and names a concrete out-of-scope action or target

9. Output Format

    - [ ] Output Format section exists (H2 `## Output Format`)
    - [ ] Fenced code block with COMPLETE report template
    - [ ] Placeholder syntax consistent (`[value]` throughout)
    - [ ] Verdict/status labels enumerated (`PASS | FAIL`, not "a status")
    - [ ] Template is copy-pasteable — no prose inside the fence
