---
applyTo: "{**/*.md,!.github/prompts/**,!.github/instructions/**,!.github/agents/**}"
---

# Documentation Conventions

1. Information Ownership

    - [ ] Main `README.md` contains a project-purpose section and a quick-start section
    - [ ] `docs/README.md` contains a link to every `docs/*.md` guide and every adapter README
    - [ ] Each `docs/*.md` file covers exactly one cross-cutting concern (architecture, auth, deployment, debugging)
    - [ ] Each `docs/*.md` file contains no forward-references that defer definitions, configuration examples, or behavioral
          descriptions to other `docs/` files
    - [ ] Each `src/**/README.md` contains Configuration, Integration, and Troubleshooting sections
    - [ ] `CHANGELOG.md` entries are grouped by version number and date

2. Single Source of Truth

    - [ ] Each configuration example appears in exactly one file — other files link to it instead of repeating it
    - [ ] Each behavioral description appears in exactly one file — other files link to it instead of repeating it
    - [ ] Main README summarizes adapter capabilities in one line + links to adapter README for details
    - [ ] `docs/` guides reference adapter READMEs for configuration specifics — not the other way around
    - [ ] No coding standards, style rules, or validation rules in documentation files

3. Cross-Reference Integrity

    - [ ] All relative links use correct Markdown link syntax and target paths consistent with the repository directory
          structure
    - [ ] Adapter READMEs link to main README and to the `/docs/` guide that covers their feature area
          using a `> Parent Documentation:` header pattern
    - [ ] No orphan documentation files — every `.md` is reachable from `docs/README.md` or main `README.md`

4. Content Accuracy

    - [ ] No source code snippets (C#, scripts, etc.) in documentation files — code is documented in source
    - [ ] No HTTP request/response examples in documentation files — API usage is documented through Swagger
    - [ ] Configuration examples contain only JSON settings structures — no inline source code
    - [ ] No fabricated configuration keys, metric names, or type references — all must correspond to codebase
          identifiers

5. Structure & Formatting

    - [ ] `README.md` and `docs/*.md` files use consistent heading hierarchy (H1 title, H2 sections, H3 subsections)
    - [ ] Limitations and known constraints documented in a dedicated section or callout block
    - [ ] No `TODO`, `FIXME`, `TBD`, or `Lorem ipsum` markers in committed documentation
    - [ ] No line exceeds 120 characters — markdown table rows are exempt
    - [ ] Prose lines that are not the last line of a paragraph contain at least 100 characters
