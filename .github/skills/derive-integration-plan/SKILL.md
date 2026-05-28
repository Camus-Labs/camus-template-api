---
name: derive-integration-plan
description: 'Derive an integration test plan from a validated user story file by identifying cross-layer boundaries and classifying existing test coverage so reviewers can approve targeted integration tests.'
argument-hint: 'Provide the path to a user story file with completed Developer Handoff Gate'
user-invocable: false
---

# Derive Integration Plan

## When to Use

- Use before writing or modifying integration tests to determine what boundaries need coverage.
- Use when a user story reaches the integration testing phase and all Developer Handoff Gate items read `Yes`.
- Use when a reviewer requires a structured test plan before the team authors integration tests for a story.

## Procedure

1. Validate that `story_file` references an existing file with all Developer Handoff Gate items reading `Yes` —
   ELSE return `FAIL` with reason describing the blocker.

2. Read the Layer Impact Matrix in Section B of `story_file` and `./docs/architecture.md` — identify which
   cross-layer boundaries the story touches (e.g., controller → service → repository → database); retain only
   boundaries whose value comes from verifying multi-layer collaboration end-to-end; discard boundaries that
   unit tests already prove in isolation.

3. Read `./src/Test/emc.camus.api.integration.test/README.md` to identify available factory variants — determine
   which factory variants the story requires based on the boundaries from Step 2.

4. Scan `./src/Test/emc.camus.api.integration.test/` for existing tests covering the retained boundaries —
   consult `./src/Test/README.md` and `./src/Test/emc.camus.api.integration.test/README.md` for project structure,
   and `./.github/instructions/testing.integration.instructions.md` for naming and structure conventions;
   classify each boundary as Existing (fully covered), Modified (test exists but misses new behavior), or
   New (no test).

5. Return `SUCCESS` — set `all_covered` to true when all boundaries are Existing; ELSE include gaps with
   factory variants, target test class, and proposed test methods for each Modified or New boundary.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  story_file: [story_file: string]
  factory_variants:
    - [factory_class_name: string]
  boundaries:
    - boundary: [cross_layer_boundary: string]
      factory: [factory_class_name: string]
      classification: [classification: Enum[Existing | Modified | New]]
      test_class: [test_class_name: string]
      test_method: [test_method_name: string]
  gaps:
    - boundary: [cross_layer_boundary: string]
      factory: [factory_class_name: string]
      target_class: [target_test_class: string]
      proposed_methods:
        - [method_name: string]
  all_covered: [all_covered: boolean]
```

```yaml
FAIL:
  story_file: [story_file_or_missing: string]
  reason: [failure_reason: string]
```

## Dependencies

- `./docs/architecture.md` — architecture reference for identifying layer boundaries.
- `./src/Test/emc.camus.api.integration.test/` — integration test project scanned for existing coverage.
- `./src/Test/README.md` — test project structure conventions.
- `./src/Test/emc.camus.api.integration.test/README.md` — integration test project conventions.
- `./.github/instructions/testing.integration.instructions.md` — integration testing conventions.
