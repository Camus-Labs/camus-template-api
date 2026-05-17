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

1. Validate that `story_file` is present and references an existing file with all Developer Handoff Gate items
   reading `Yes` — if missing, unreadable, or any gate item is `No`, return `FAIL` with reason describing the
   blocker; otherwise proceed to Step 2.

2. Derive integration test scope from the validated `story_file` (using its Layer Impact Matrix in Section B),
   `./docs/architecture.md`, and the factory variants in `./src/Test/emc.camus.api.integration.test/` — determine
   which factory variants the story requires based on the configuration variants the changes affect; proceed to
   Step 3.

3. List the cross-layer boundaries the story touches (e.g., controller → service → repository → database, HTTP
   pipeline → middleware → response) and filter to retain only boundaries whose value comes from verifying that
   multiple layers collaborate correctly end-to-end; discard boundaries that unit tests already prove in isolation;
   proceed to Step 4.

4. Scan `./src/Test/emc.camus.api.integration.test/` for existing tests that cover the retained boundaries —
   consult `./src/Test/README.md` and `./src/Test/emc.camus.api.integration.test/README.md` for project structure,
   consult `./.github/instructions/testing.instructions.md` and
   `./.github/instructions/testing.integration.instructions.md` for test conventions, and consult
   `./docs/README.md` for layer and adapter contracts; read test files and match against the factory variants from
   Step 2; classify each boundary as Existing (test exercises the boundary), Modified (test exists but does not
   cover the new behavior), or New (no test for this boundary); proceed to Step 5.

5. Return `SUCCESS` with an empty `gaps` list and `all_covered` set to `true` when all boundaries are Existing;
   otherwise return `SUCCESS` with the factory variants, classified boundaries, and a proposed test plan listing
   each Modified or New boundary with its target test class and test methods.

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
- `./.github/instructions/testing.instructions.md` — general testing conventions.
- `./.github/instructions/testing.integration.instructions.md` — integration testing conventions.
- `./docs/README.md` — layer and adapter contract references.
