---
applyTo: ".github/instructions/*.instructions.md"
---

# Instructions File Conventions

1. Structure

    - [ ] Valid YAML `---` frontmatter containing an `applyTo` glob pattern — instruction files require `applyTo`
    - [ ] `applyTo` targets the narrowest scope that covers all files the checks apply to
    - [ ] H1 title describes the scope (e.g., "API Layer Conventions", "C# Coding Standards")
    - [ ] No prose introduction between H1 and first numbered section
    - [ ] Numbered sections as categories (e.g., `1. Scope Compliance`, `2. Type Conventions & Lifecycle`)
    - [ ] Checks as `- [ ]` items indented under their section
    - [ ] Multi-line checks use continuation indent aligned with the first character after `- [ ]`

2. Check Quality

    - [ ] Each check is one falsifiable statement — can be verified true or false against a file
    - [ ] Active voice, declarative (e.g., "No business logic") — not "Should avoid business logic"
    - [ ] Inline examples, when present, appear in parentheses (e.g., `rule (example)`)
    - [ ] Rationale or contrast, when present, after em-dash (e.g., `rule — rationale`)
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "when appropriate")

3. Scope & Overlap

    - [ ] No check duplicated across files whose `applyTo` globs overlap — each rule has exactly one owner per
          matched file; files with non-overlapping scopes may contain identical checks
    - [ ] Cross-cutting checks reside in the language-scope file (e.g.,
          `.github/instructions/csharp.instructions.md`) — not in layer-specific files (e.g.,
          `.github/instructions/api.instructions.md`)
    - [ ] Layer-specific checks reside in their layer file (e.g.,
          `.github/instructions/api.instructions.md`) — not in the language-scope file
    - [ ] Specialized instruction files (e.g., `.github/instructions/api.instructions.md`) do not restate rules
          already covered by broader-scope instruction files (e.g., `.github/instructions/csharp.instructions.md`)
          whose `applyTo` glob is a superset of theirs
    - [ ] Checks reference concrete file paths or glob patterns — not abstract references to other instruction files

4. Section Naming

    - [ ] Section names describe the category of checks they contain — not generic labels like "General" or "Other"
    - [ ] Sections numbered sequentially starting at 1

5. Boundary Violations

    - [ ] No checks that describe process workflows or review procedures — those belong in prompts or agents
    - [ ] No checks that require running the application to verify — checks validate static code/file properties
    - [ ] No checks scoped to files outside the `applyTo` glob
