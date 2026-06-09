---
name: markdown-lint
description: 'Run markdownlint-cli2 --fix on .md files and return lint results for use when fixing or verifying markdown formatting compliance.'
argument-hint: 'Provide one or more workspace-relative paths to .md files, or "all" to lint the entire solution'
user-invocable: false
---

# Markdown Lint

## When to Use

- Auto-fix `.md` formatting violations after edits.
- Verify markdown formatting compliance during review workflows.

## Procedure

1. Receive one or more workspace-relative `.md` file paths as input, or the keyword `all`.
2. Determine the target:
   - If the argument is `all`, use the glob pattern `"**/*.md"` to lint every `.md` file in the
     workspace — rely on the repo-root `.markdownlint-cli2.jsonc` config for rule configuration.
   - Otherwise, quote each individual path.
3. Run in terminal:
   - All files: `npx markdownlint-cli2 --fix "**/*.md"`
   - Specific files: `npx markdownlint-cli2 --fix "<path1>" "<path2>" ...`
4. Evaluate the exit code: if 0, return `SUCCESS`; otherwise, capture stdout/stderr, parse each
   remaining violation (file, line, rule, description), and return a structured `FAIL` result with
   unfixed findings.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  files_fixed: [file_count: integer]
```

```yaml
FAIL:
  findings:
    - file: [file_path: string]
      line: [line_number: integer]
      rule: [rule_code: string]
      description: [violation_description: string]
```

## Dependencies

- `npx` — Node.js package runner (bundled with npm >= 5.2)
- `markdownlint-cli2` — markdown linter CLI
