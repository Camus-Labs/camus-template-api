---
name: close-coverage-gaps
description: 'Run the coverage report and add unit tests until every production file reaches 100% line and branch coverage to deliver an exhaustively covered solution.'
argument-hint: 'No arguments — operates on the entire solution'
user-invocable: false
---

# Close Coverage Gaps

## When to Use

- Drive every production file to 100% line and branch coverage before a release sign-off.
- Eliminate coverage gaps surfaced by the QA Tester agent during release validation.

## Procedure

1. Refresh coverage by running the VS Code task `test-refresh-coverage-report`; if the task fails, return
   `FAIL` with `reason: "coverage report generation failed"` and the task output; otherwise proceed.
2. Parse `coveragereport/index.html` (or the latest cobertura XML under `src/Test/**/TestResults/`) to list
   every production file whose line coverage or branch coverage is below 100%, capturing the file path and
   the specific uncovered lines and branches.
3. Iterate over each uncovered file up to 10 files per run — for each file, add or modify unit test methods
   under `src/Test/<matching>.test/` following `.github/instructions/testing.unit.instructions.md`; record
   every test method created or modified with its file path and test name.
4. Run the VS Code task `test-unit` after each file is addressed; on failure, fix the failing tests and
   re-run up to 3 iterations; if failures persist after 3 iterations, return `FAIL` with `reason:
   "unit tests fail after gap closure"` and the failing test names; otherwise proceed to the next file.
5. Refresh coverage again by running the VS Code task `test-refresh-coverage-report` after all files are
   addressed; parse the report and confirm every production file reaches 100% line and branch coverage; ELSE
   return `PARTIAL` with the remaining gaps and the tests added so far.
6. Return `SUCCESS` with the list of tests added or modified and the final coverage totals.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  files_analyzed: [file_count: integer]
  files_line_coverage_100: [file_count: integer]
  files_branch_coverage_100: [file_count: integer]
  tests_added:
    - file: [test_file_path: string]
      name: [test_method_name: string]
  tests_modified:
    - file: [test_file_path: string]
      name: [test_method_name: string]
```

```yaml
PARTIAL:
  files_analyzed: [file_count: integer]
  files_line_coverage_100: [file_count: integer]
  files_branch_coverage_100: [file_count: integer]
  remaining_gaps:
    - file: [production_file_path: string]
      uncovered_lines: [line_numbers: string]
      uncovered_branches: [branch_descriptions: string]
  tests_added:
    - file: [test_file_path: string]
      name: [test_method_name: string]
  tests_modified:
    - file: [test_file_path: string]
      name: [test_method_name: string]
```

```yaml
FAIL:
  reason: [failure_description: string]
  details: [diagnostic_output: string]
```

## Dependencies

- `test-refresh-coverage-report` — VS Code task that runs unit tests with coverage and regenerates the HTML report
- `test-unit` — VS Code task that runs the unit test suite
- `coveragereport/index.html` — generated coverage report consumed for gap analysis
- `src/Test/**/TestResults/**/coverage.cobertura.xml` — raw coverage data file
- `.github/instructions/testing.unit.instructions.md` — unit test conventions followed when writing new tests
