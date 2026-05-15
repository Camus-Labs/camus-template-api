---
name: concurrent-review
description: 'Dispatch three parallel sub-agent reviews and merge results into a deduplicated report when orchestrating multi-model code or documentation reviews against a resolved file list.'
argument-hint: 'Provide a review prompt path and a list of files to review'
user-invocable: false
---

# Concurrent Review

## When to Use

- Use after resolving a scope to a file list when the task requires multi-model evaluation.
- Use to orchestrate parallel sub-agent reviews with different models against the same file set.
- Use to merge and deduplicate findings from multiple review models into a single report.

## Procedure

1. Validate that `prompt_path` is present and references an existing `.prompt.md` file — if missing, return `FAIL`
   with reason `"prompt_path argument is missing"`; validate that `files` is a non-empty list — if empty, return
   `FAIL` with reason `"files list is empty"`; otherwise proceed to Step 2.

2. Dispatch three parallel sub-agents (`CodexReviewer`, `SonnetReviewer`, `OpusReviewer`) via the `agent` tool,
   each passing the file at `prompt_path` and the `files` list as parameter for the prompt file — collect the
   full review report from each sub-agent.

3. Evaluate sub-agent results — if all three fail to return a complete report, return `FAIL` with reason
   `"all sub-agents failed"` and include each failure reason; if one or two fail, record each failure for the
   Failed Agents section and proceed to Step 4 with the successful reports only; if all three succeed, proceed to
   Step 4 with all reports.

4. Merge the successful sub-agent reports into a single deduplicated findings list — mark a section FAIL if any
   successful model marks it FAIL; otherwise mark it PASS; if two or more sub-agents flag the same checklist item
   on the same file, record it once and note which models flagged it; otherwise (single model), still include it;
   mark columns of failed sub-agents as N/A in the Checklist Results table.

5. Validate each merged finding against the full rule text — re-read the exact checklist item including all
   exception clauses; discard any finding where the flagged content falls under an explicit exception in the rule;
   note each discarded finding and the exception clause that applies; if discarding changes a section from FAIL to
   zero findings, flip that section to PASS; otherwise leave the section verdict unchanged.

6. Return `SUCCESS` with the merged checklist results, validated findings, discarded findings, failed agents, and
   model self-reported identifiers.

## Output Contract

Return exactly one of:

```yaml
SUCCESS:
  prompt_path: [prompt_path: string]
  files_count: [files_count: integer]
  models:
    - agent: CodexReviewer
      declared: codex
      self_reported: [self_reported_model: string]
    - agent: SonnetReviewer
      declared: claude-sonnet
      self_reported: [self_reported_model: string]
    - agent: OpusReviewer
      declared: claude-opus
      self_reported: [self_reported_model: string]
  checklist_results:
    - section: [section_name: string]
      source_instruction: [instruction_file: string]
      codex: [codex_verdict: PASS | FAIL | N/A]
      sonnet: [sonnet_verdict: PASS | FAIL | N/A]
      opus: [opus_verdict: PASS | FAIL | N/A]
      merged: [merged_verdict: PASS | FAIL]
  findings:
    - section: [section_number: integer]
      file: [file_path: string]
      issue: [issue_description: string]
      flagged_by: [flagged_by_models: list<string>]
      evidence: [evidence_text: string]
      fix: [corrective_action: string]
  discarded_findings:
    - section: [section_number: integer]
      file: [file_path: string]
      issue: [issue_description: string]
      flagged_by: [flagged_by_models: list<string>]
      exception_clause: [exception_clause_text: string]
  failed_agents:
    - agent: [agent_name: string]
      reason: [failure_reason: string]
```

```yaml
FAIL:
  prompt_path: [prompt_path: string]
  reason: [failure_explanation: string]
```

## Dependencies

- `agent` — Invoke parallel reviewers in Steps 2–3 via the VS Code Copilot sub-agent dispatch tool.
