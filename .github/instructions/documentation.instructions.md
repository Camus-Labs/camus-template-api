---
applyTo: "**/*.md"
---

# Documentation Conventions

1. Information Ownership

    - [ ] Main `README.md`: project overview, feature list, project structure, quick-start — links to `/docs/` and
      adapter READMEs for details
    - [ ] `docs/README.md`: documentation hub — index of all guides with brief summaries and cross-links
    - [ ] `docs/*.md`: architecture deep-dives, authentication flows, deployment guides, debugging workflows
    - [ ] `src/**/README.md` (adapters, layers, infrastructure): detailed usage guide for that specific component —
      configuration, code samples, API reference
    - [ ] `CHANGELOG.md`: version history with dated entries — every user-facing change has an entry

2. Single Source of Truth

    - [ ] Each fact lives in exactly one file — other files link to it, never restate it
    - [ ] Main README summarizes adapter capabilities in one line + links to adapter README for details
    - [ ] `docs/` guides reference adapter READMEs for configuration specifics — not the other way around
    - [ ] Adapter READMEs link back to parent documentation (`> Parent Documentation:` header pattern)
    - [ ] No coding standards, validation rules, or architectural conventions in documentation — documentation describes
      usage and behavior, not how to write code

3. Cross-Reference Integrity

    - [ ] All relative links use correct paths and resolve to existing files
    - [ ] Adapter READMEs link to main README and relevant `/docs/` guide
    - [ ] `docs/README.md` links to every adapter README and `/docs/` guide
    - [ ] No orphan documentation files — every `.md` is reachable from `docs/README.md` or main `README.md`

4. Content Accuracy

    - [ ] Configuration examples match actual `appsettings.json` structure and property names
    - [ ] Code samples match current implementation signatures and namespaces
    - [ ] Metric/counter names match implementation
    - [ ] No references to removed features or renamed types

5. Structure & Formatting

    - [ ] `README.md` and `docs/*.md` files use consistent heading hierarchy (H1 title, H2 sections, H3 subsections)
    - [ ] Limitations and known constraints documented in a dedicated section or callout block
    - [ ] No stale TODOs or placeholder content in committed documentation
