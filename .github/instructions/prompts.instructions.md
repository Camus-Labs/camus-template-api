---
applyTo: ".github/prompts/**/*.prompt.md"
---

# Prompts Development Conventions

1. Writing Quality and Structure

    - [ ] Sections appear in canonical order: Frontmatter → Goal → Context → Inputs → Process → Rules → Output Format
    - [ ] Title (H1) is present and starts with a verb or a domain noun
    - [ ] No extra top-level sections outside the canonical set
    - [ ] Heading hierarchy is correct (H1 title, H2 sections, NO H3+ subsections)
    - [ ] No two sections contain the same constraint, instruction, or definition
    - [ ] No sentence contains a "to be" + past-participle construction ("is generated", "should be done", "will be returned")
    - [ ] Every process step output appears in a later step or in the output template
    - [ ] All prose uses active voice — no passive constructions ("is generated", "was done")
    - [ ] All prose uses imperative mood — no indicative constructions ("the step generates", "it returns")

2. Frontmatter

    - [ ] Valid YAML `---` frontmatter present
    - [ ] `description` field contains exactly one sentence
    - [ ] `description` sentence starts with a present-tense verb
    - [ ] `description` sentence contains a direct object
    - [ ] `description` sentence ends with a purpose clause
    - [ ] `description` field value is at most 1024 characters
    - [ ] `agent` field present
    - [ ] `agent` field is one of: `agent`, `ask`, `edit`
    - [ ] `argument-hint` field present
    - [ ] `argument-hint` value describes how to invoke the prompt
    - [ ] No under-declared tools — used in steps but not listed

3. Goal

    - [ ] Goal section exists (H2 `## Goal`)
    - [ ] Outcome names a concrete artifact type (e.g., report, file, fix, or plan)
    - [ ] `**Success:**` line exists and defines the completion condition as an imperative statement
    - [ ] `**Failure:**` line exists and defines the abort conditions as an imperative statement
    - [ ] Goal section contains exactly one artifact noun — no conjunctions ("and", "or") joining distinct artifact types

4. Context

    - [ ] Context section exists when any process step references a `#file:` or external data source
    - [ ] Lists required files using `#file:` references
    - [ ] At least one process step consumes every listed context item
    - [ ] Every `#file:` reference targets an existing workspace file

5. Inputs

    - [ ] Inputs section exists (H2 `## Inputs`)
    - [ ] Required inputs include name, format, and type
    - [ ] Optional inputs carry explicit defaults
    - [ ] No dead inputs (listed but never consumed in process or output)
    - [ ] No phantom inputs (consumed in process but never declared)

6. Process

    - [ ] Process section exists (H2 `## Process`)
    - [ ] Steps are numbered
    - [ ] Logical dependency order (later steps use earlier outputs)
    - [ ] Each step starts with ONE action verb
    - [ ] 3–8 total steps
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
    - [ ] Conditionals have explicit ELSE or default — guard-and-stop branches where the only alternative is
          continuation to the next numbered step satisfy this rule implicitly
    - [ ] Loops have max-iteration bound
    - [ ] Explicit stopping criterion
    - [ ] First step validates inputs
    - [ ] Last step produces the output
    - [ ] No step contains a clause that duplicates or contradicts text in another step or rule
    - [ ] Every process-computed value has a placeholder in the template
    - [ ] Procedure steps that build, test, or run the application use workspace task labels (e.g., `build`, `test-all`,
          `test-unit`, `test-integration`, `run-api`) — not raw `dotnet` commands

7. Rules

    - [ ] Rules section exists (H2 `## Rules`)
    - [ ] Each rule is one imperative sentence (MUST X / MUST NOT Y)
    - [ ] Rules are falsifiable (can be verified true or false)
    - [ ] At least one scope-limiting rule
    - [ ] No rule contains a clause that duplicates or contradicts text in another rule

8. Output Format

    - [ ] Output Format section exists (H2 `## Output Format`)
    - [ ] Fenced code block with COMPLETE report template
    - [ ] Placeholder syntax consistent (`[value]` throughout)
    - [ ] Verdict/status labels listed explicitly (`PASS | FAIL`, not "a status")
    - [ ] Template is copy-pasteable — no prose inside the fence
