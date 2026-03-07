---
applyTo: ".github/prompts/**"
---

# Prompts Development Conventions

0. Writing Quality and Structure

   - [ ] Sections appear in canonical order: Frontmatter → Goal → Context → Inputs → Process → Rules → Output Format
   - [ ] Title (H1) is present and describes the prompt's purpose
   - [ ] No extra top-level sections outside the canonical set
   - [ ] Heading hierarchy is correct (H1 title, H2 sections, NO H3+ subsections)
   - [ ] No over-limit 120 characters lines limit
   - [ ] No under-utilized line width — line breaks should occur as close as possible to the 120-character limit
   - [ ] No redundant sections or repeated information
   - [ ] Active voice, imperative mood
   - [ ] No orphan outputs — every process step output feeds a downstream step or the final output template
   - [ ] No cross-section contradictions — no section makes a claim that another section negates

1. Frontmatter

   - [ ] Valid YAML `---` frontmatter present
   - [ ] `description` — one sentence: verb + object + outcome
   - [ ] `mode` — declares the execution mode
   - [ ] `tools` — lists ONLY tools the process steps actually use
   - [ ] `argument-hint` — included, describes how to invoke the prompt
   - [ ] No over-privileged tools (listed but never used in steps)
   - [ ] No under-declared tools (used in steps but not listed)

2. Goal

   - [ ] Goal section exists (H2 `## Goal`)
   - [ ] Concrete outcome stated (report, file, fix, plan)
   - [ ] Success criteria are binary-testable
   - [ ] Failure conditions are explicit
   - [ ] Goal describes a single deliverable — not multiple unrelated outcomes

3. Context

   - [ ] Section exists if the prompt needs external references or data
   - [ ] Lists required files using `#file:` references
   - [ ] References are specific, not broad (section-level when applicable)
   - [ ] Minimal — only what the process steps consume
   - [ ] No stale references (every referenced file exists and is relevant)

4. Inputs

   - [ ] Inputs section exists (H2 `## Inputs`)
   - [ ] Required inputs listed with name, format, and type
   - [ ] Optional inputs marked with explicit defaults
   - [ ] No dead inputs (listed but never consumed in process or output)
   - [ ] No phantom inputs (consumed in process but never declared)

5. Process

   - [ ] Process section exists (H2 `## Process`)
   - [ ] Steps are numbered
   - [ ] Logical dependency order (later steps use earlier outputs)
   - [ ] Each step starts with ONE action verb
   - [ ] 3–8 total steps
   - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
   - [ ] Conditionals have explicit ELSE or default
   - [ ] Loops have max-iteration bound
   - [ ] Explicit stopping criterion
   - [ ] First step validates inputs; last step produces the output
   - [ ] One bounded action per step — no step performs multiple independent evaluations
   - [ ] Steps that need tools name them explicitly
   - [ ] No step contradicts another step or a rule

6. Output Format

   - [ ] Output Format section exists (H2 `## Output Format`)
   - [ ] Fenced code block with COMPLETE report template
   - [ ] Placeholder syntax consistent (`[value]` throughout)
   - [ ] Verdict/status labels enumerated (`PASS | FAIL`, not "a status")
   - [ ] Template is copy-pasteable — no prose inside the fence
   - [ ] Every process-computed value has a placeholder in the template
   - [ ] Sufficient for downstream handoff without re-reading source

7. Rules

   - [ ] Rules section exists (H2 `## Rules`)
   - [ ] Each rule is one imperative sentence (MUST X / MUST NOT Y)
   - [ ] Rules are falsifiable (can be verified true or false)
   - [ ] At least one scope-limiting rule
   - [ ] No rule contradicts or duplicates a process step
   - [ ] No rule is unenforceable or purely aspirational
