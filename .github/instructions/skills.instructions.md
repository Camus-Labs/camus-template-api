---
applyTo: ".github/skills/**/SKILL.md"
---

# Skills Development Conventions

1. Structure

    - [ ] Skill lives in `.github/skills/<name>/` with a `SKILL.md` at the root
    - [ ] Folder name matches the `name` field in SKILL.md frontmatter exactly
    - [ ] Folder name contains only lowercase alphanumeric characters and hyphens
    - [ ] No files outside `SKILL.md`, `scripts/`, `references/`, or `assets/` subdirectories
    - [ ] `SKILL.md` body is under 500 lines — large content belongs in `references/`
    - [ ] File references use relative paths with `./` prefix (e.g., `[script](./scripts/run.sh)`)

2. Frontmatter

    - [ ] Valid YAML `---` frontmatter present
    - [ ] `name` field present
    - [ ] `name` value matches the skill folder name exactly
    - [ ] `name` value contains only lowercase alphanumeric characters and hyphens
    - [ ] `description` field contains exactly one sentence
    - [ ] `description` sentence follows verb + object + outcome structure
    - [ ] `description` field value is at most 1024 characters
    - [ ] `argument-hint` field present
    - [ ] `argument-hint` value describes how to invoke the skill
    - [ ] Frontmatter contains only keys from: `name`, `description`, `argument-hint`, `user-invocable`

3. Body Sections

    - [ ] H1 title present
    - [ ] "When to Use" section exists (H2)
    - [ ] "When to Use" section lists concrete trigger scenarios as bullet points
    - [ ] "Procedure" section exists (H2)
    - [ ] "Procedure" section contains numbered steps describing the execution workflow
    - [ ] "Output Contract" section exists (H2)
    - [ ] "Output Contract" section defines the exact return format the caller receives
    - [ ] No extra top-level sections outside: title, When to Use, Procedure, Output Contract, Dependencies
    - [ ] Heading hierarchy uses H1 for the title and H2 for sections — no H3+ subsections

4. Procedure Quality

    - [ ] Steps number sequentially starting at 1
    - [ ] Each step starts with one action verb
    - [ ] 2–8 total steps
    - [ ] No vague qualifiers ("as needed", "consider", "optionally", "may")
    - [ ] Commands appear inside inline code spans or fenced code blocks — no bare shell commands in prose
    - [ ] Conditionals have explicit ELSE or default
    - [ ] Terminal commands quote variable paths to handle spaces

5. Output Contract

    - [ ] Defines all possible return shapes (success and failure)
    - [ ] Uses a fenced code block for structured output format
    - [ ] All placeholders use identical naming syntax (e.g., `[snake_case]`)
    - [ ] Each return value placeholder has an explicit type annotation in the fenced code block

6. Self-Containment

    - [ ] Every external CLI tool or file referenced in the Procedure section appears by exact name
          in a `## Dependencies` section
    - [ ] No references to files outside the skill folder (workspace root config files excepted)

7. Writing Quality

    - [ ] All prose uses active voice, imperative mood — no passive constructions
    - [ ] Description field contains at least one use-case verb and one domain noun
