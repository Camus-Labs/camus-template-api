---
name: validate-handoff-gate
description: 'Verify every checklist item under a named handoff gate in a story or release file reads Yes or N/A to gate downstream agent invocation.'
argument-hint: 'Provide story_file (path) and gate_name (e.g., "Architect Handoff Gate")'
user-invocable: false
---

# Validate Handoff Gate

## When to Use

- Position any worker agent (architect, tester.unit, developer, tester.integration, tester.qa,
  technical_writer, release_manager) at its first step to fail-fast when its upstream gate is unsigned.
- Confirm a gate the current agent just populated reads `Yes` or `N/A` on every item before producing the
  handoff report.

## Procedure

1. Validate inputs — if `story_file` is empty or the file does not exist, return `FAIL` with `reason:
   "story_file missing"`; if `gate_name` is empty, return `FAIL` with `reason: "gate_name missing"`;
   otherwise proceed.
2. Locate the gate section by running `awk -v g="### $gate_name" 'index($0,g)==1{f=1;next} f && /^###? /{exit}
   f{print}' "$story_file"` and capture the lines as `gate_block` — on empty `gate_block`, return `FAIL` with
   `reason: "gate $gate_name not found in $story_file"`; otherwise proceed.
3. Extract every checklist item by selecting lines from `gate_block` that match `^-` and excluding any line
   whose text after the first `:` contains `sign-off` (case-insensitive); store the result as `items` — on
   empty `items`, return `FAIL` with `reason: "gate $gate_name has no checklist items"`; otherwise proceed.
4. Evaluate every item — for each line in `items`, extract the value inside the first pair of backticks after
   the `:` separator; treat the item as failing when no backtick value exists, when the value reads `No`,
   when the value contains `[Yes | No]` (template placeholder), or when the value is empty; otherwise treat
   the item as passing; collect each failing item's leading label (the text between `-` and `:`) into `blockers`.
5. Evaluate the sign-off line — locate the line in `gate_block` whose label ends with `sign-off`; capture the
   value inside the first pair of backticks; treat sign-off as failing when no value exists, when the value
   reads `[Name, Date]`, or when the value is empty; otherwise treat the sign-off as passing; append
   `"<gate_name> sign-off"` to `blockers` on failure.
6. Return the result — if `blockers` is empty, return `SUCCESS` with `story_file`, `gate_name`, and
   `items_checked` (count); ELSE return `FAIL` with `story_file`, `gate_name`, `reason: "gate $gate_name has
   unsatisfied items"`, and `blockers` listing every failing label.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  story_file: [story_file: string]
  gate_name: [gate_name: string]
  items_checked: [items_checked: integer]
```

```yaml
FAIL:
  story_file: [story_file_or_missing: string]
  gate_name: [gate_name_or_missing: string]
  reason: [reason: string]
  blockers: [blockers: list<string> | empty]
```

## Dependencies

- `awk` — POSIX text processor; use to isolate the gate section between its heading and the next H2/H3.
