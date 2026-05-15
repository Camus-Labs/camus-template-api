---
applyTo: "{README.md,CHANGELOG.md,CONTRIBUTING.md,SECURITY.md,docs/**/*.md,src/**/README.md}"
---

# Documentation Conventions

1. Information Ownership

    - [ ] Main `README.md` contains a project-purpose section
    - [ ] Main `README.md` contains a quick-start section
    - [ ] `docs/README.md` contains a link to every `docs/*.md` guide and every adapter README
    - [ ] Each `docs/*.md` file has exactly one H1 heading
    - [ ] No `docs/*.md` file contains an H2 heading that matches the H1 title of another `docs/*.md` file
    - [ ] Each `src/**/README.md` contains a Configuration section
    - [ ] Each `src/**/README.md` contains an Integration section
    - [ ] Each `src/**/README.md` contains a Troubleshooting section
    - [ ] `CHANGELOG.md` entries are grouped by version number and date

2. Single Source of Truth

    - [ ] No configuration example duplicated verbatim from another file matched by this glob — link to the
          canonical location instead
    - [ ] No runtime feature behavior description duplicated verbatim from another file matched by this glob — link
          to the canonical location instead
    - [ ] Main README summarizes each adapter's capabilities in one line
    - [ ] Main README links to `src/Adapters/**/README.md` for each adapter's details
    - [ ] `docs/` guides reference `src/Adapters/**/README.md` for configuration specifics — not the other way around
    - [ ] No coding standards, style rules, or validation rules in documentation files

3. Cross-Reference Integrity

    - [ ] All relative links use correct Markdown link syntax and target paths consistent with the repository directory
          structure
    - [ ] `src/Adapters/**/README.md` files link to main README
    - [ ] `src/Adapters/**/README.md` files link to the `/docs/` guide covering their feature area
          (e.g., `> Parent Documentation:` header)
    - [ ] `docs/README.md` or main `README.md` links to every file matched by this glob (directly or
          transitively) — no orphan documentation files

4. Content Accuracy

    - [ ] No source code snippets (C#, scripts, etc.) in documentation files — code is documented in source
    - [ ] No HTTP request/response examples in documentation files — API usage is documented through Swagger
    - [ ] Configuration examples contain only JSON settings structures — no inline source code
    - [ ] No fabricated configuration keys, metric names, or type references — all must correspond to codebase
          identifiers

5. Structure & Formatting

    - [ ] Each file has exactly one H1 heading
    - [ ] Heading levels are not skipped (H1 → H2 → H3)
    - [ ] No `TODO`, `FIXME`, `TBD`, or `Lorem ipsum` markers in committed documentation
