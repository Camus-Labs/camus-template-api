---
applyTo: ".github/instructions/**"
---

# Instructions File Conventions

1. Structure

    - [ ] Valid YAML `---` frontmatter with `applyTo` glob pattern
    - [ ] `applyTo` targets the narrowest scope that covers all files the checks apply to
    - [ ] H1 title describes the scope (e.g., "API Layer Conventions", "C# Coding Standards")
    - [ ] No prose introduction between H1 and first numbered section
    - [ ] Numbered sections as categories (e.g., `1. Scope Compliance`, `2. Type Conventions & Lifecycle`)
    - [ ] Checks as `- [ ]` items indented under their section
    - [ ] Multi-line checks use continuation indent aligned with the first character after `- [ ]`

2. Check Quality

    - [ ] Each check is one falsifiable statement — can be verified true or false against a file
    - [ ] Active voice, declarative ("No business logic" not "Should avoid business logic")
    - [ ] Concrete — names specific patterns, types, or methods (e.g., `ArgumentNullException.ThrowIf*()`)
    - [ ] Dash separator between rule and rationale/example when present (e.g., `rule — rationale`)
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "when appropriate")
    - [ ] No aspirational checks that cannot be mechanically verified

3. Scope & Overlap

    - [ ] No check duplicated across instruction files — each rule has exactly one owner
    - [ ] Cross-cutting checks in `csharp.instructions.md` — layer-specific checks in layer files
    - [ ] Specialized instruction files (e.g., `adapters.persistence.instructions.md`) do not restate parent file
      checks (`adapters.instructions.md`)
    - [ ] Checks reference concrete boundaries, not other instruction files

4. Section Naming

    - [ ] Use domain-appropriate section names
    - [ ] Sections numbered sequentially starting at 1

5. Boundary Violations

    - [ ] No checks that describe process workflows or review procedures — those belong in prompts or agents
    - [ ] No checks that require running the application to verify — checks validate static code/file properties
    - [ ] No checks scoped to files outside the `applyTo` glob
