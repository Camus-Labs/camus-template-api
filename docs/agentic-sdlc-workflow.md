# Feature Development Workflow

Agent-driven workflow for implementing features in the Camus API. Each phase produces a deliverable that feeds
the next, with human approval gates between phases. Agents are invoked with `@name` in Copilot Chat.

## Workflow Overview

``` text
┌──────────────────────────────────────────────────────────────────────────┐
│                        FEATURE DEVELOPMENT PIPELINE                      │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Phase 0: PRODUCT OWNER ──── @product_owner                              │
│  │  Input:  Feature request (free text)                                  │
│  │  Action: Create feat_ branch from latest main                         │
│  │  Output: docs/stories/todo/{slug}/US-*.md (Section A)                 │
│  │  Gate:   Human reviews & approves stories                             │
│  ▼                                                                       │
│  Phase 1: ARCHITECT ──────── @architect                                  │
│  │  Input:  Story file with completed Section A                          │
│  │  Output: Section B populated (architecture + implementation plan)     │
│  │  Gate:   Human reviews & approves architecture                        │
│  ▼                                                                       │
│  Phase 2: TESTER ─────────── @unit.tester                                │
│  │  Input:  Story file with completed Sections A + B                     │
│  │  Output: Stubs + test files (TDD red) + Section C                     │
│  │  Gate:   Human reviews test design + production skeleton              │
│  ▼                                                                       │
│  Phase 3: DEVELOPER ──────── @developer                                  │
│  │  Input:  Story file with Section C tests in RED                       │
│  │  Output: Implementation (TDD green) + code review approved            │
│  │  Sub:    review.code.prompt.md (invoked automatically)                │
│  │  Gate:   Human reviews implementation                                 │
│  ▼                                                                       │
│  Phase 4: INTEGRATION ────── @integration.tester                         │
│  │  Input:  Story file with Developer Handoff Gate complete              │
│  │  Output: Integration tests + Section D + status report                │
│  │  Gate:   Human reviews findings, approves integration status          │
│  ▼                                                                       │
│  Phase 5: REVIEW ─────────── @concurrent.reviewer.code + docs            │
│  │  Input:  Uncommitted files from previous phases                       │
│  │  Step 1: @concurrent.reviewer.code branch (multi-model review)        │
│  │  Step 2: @concurrent.reviewer.documentation branch (docs review)      │
│  │  Gate:   Human reviews reports, approves final quality                │
│  ▼                                                                       │
│  Phase 6: TECHNICAL WRITER ─ @technical_writer                           │
│  │  Input:  Story file with Review phase complete (both PASS)            │
│  │  Output: Version + CHANGELOG + Swagger + Postman + XML docs + lint    │
│  │  Gate:   Human reviews documentation updates                          │
│  ▼                                                                       │
│  Phase 7: QA TESTER ──────── @qa.tester                                  │
│  │  Input:  Story file with Technical Writer Handoff Gate complete       │
│  │  Step 1: Full test suite + coverage gap analysis                      │
│  │  Step 2: Write coverage tests (user-approved) + code review           │
│  │  Step 3: Guide local validation (Docker + Postman) — human step       │
│  │  Gate:   Human confirms local validation passed                       │
│  ▼                                                                       │
│  Phase 8: RELEASE MANAGER ── @release_manager                            │
│  │  Input:  Story file with QA Tester Handoff Gate complete             │
│  │  Step 1: Commit and push branch                                       │
│  │  Step 2: Create PR to main                                            │
│  │  Step 3: Create release (if version ≠ tag + user confirms)            │
│  │  Step 4: Create deploy/dev + deploy/prod PRs                          │
│  │  Gate:   All PRs created and ready                                    │
│  ▼                                                                       │
│  DONE ✓                                                                  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Phases in Detail

### Phase 0: Product Owner — `@product_owner`

**Invoke:** `@product_owner` → describe the feature request in free text.

Example: `@product_owner I need CRUD operations for managing user profiles with email validation`

**What the agent does:**

1. Creates a feature branch from the latest `main` (`feat_{request-slug}`), confirming the branch name with you
2. Decomposes the feature request into atomic user stories
3. Creates story files under `docs/stories/todo/{request-slug}/` using the
   [story template](stories/_user_story_template.md)
4. Populates Section A (Product Owner Definition) — story statement, scope, FRs, NFRs, ACs
5. Asks up to 5 rounds of clarification questions for ambiguous requirements
6. Evaluates the Product Owner Handoff Gate per story

**Deliverable:** Feature branch created, story files with completed Section A, handoff gate evaluated, status set.

**Your role:** Confirm the branch name. Review stories. Verify scope boundaries, acceptance criteria completeness,
and NFR coverage. Approve or request changes before handing to the architect.

---

### Phase 1: Architect — `@architect`

**Invoke:** `@architect` → provide the path to a story file with completed Section A.

Example: `@architect #file:docs/stories/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Product Owner Handoff Gate is fully passing
2. Maps functional requirements to layers (Domain, Application, API, Adapters)
3. Populates Section B — Layer Impact Matrix, Cross-Cutting Concern Decisions,
   Layer-Based Implementation Plan, Traceability table
4. Asks up to 5 rounds of clarification for architectural ambiguities
5. Evaluates the Architect Handoff Readiness gate

**Deliverable:** Story file with completed Section B, implementation plan ordered by dependency direction,
and status set to `READY_FOR_IMPLEMENTATION`.

**Your role:** Review architecture. Verify layer boundaries, implementation plan order, and cross-cutting
decisions. Approve before proceeding to testing.

---

### Phase 2: Tester — `@unit.tester`

**Invoke:** `@unit.tester` → provide the path to a story file with completed Sections A and B.

Example: `@unit.tester #file:docs/stories/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Architect Handoff Readiness gate is fully passing
2. Reads context files and extracts Acceptance Criteria (Section A) and Layer Impact Matrix (Section B)
3. Scaffolds production stubs from the Layer Impact Matrix (interfaces, models, empty bodies)
4. Builds to verify stubs compile, fixing compilation errors up to 5 times
5. Presents the production skeleton for user review (up to 5 review cycles)
6. Creates test classes mapping each AC to test methods (xUnit + FluentAssertions + Moq, AAA pattern)
7. Verifies tests compile and fail for the right reason (TDD red), redesigning tests up to 5 times if a stub
   accidentally satisfies one
8. Populates Section C — Skeleton Inventory table, Test Traceability table, and evaluates the Tester Handoff Gate

**Deliverable:** Stub files, test files, Section C populated (Skeleton Inventory + Test Traceability),
Tester Handoff Report.

**Your role:** Review the production skeleton and test design. Verify coverage of every AC, proper naming,
behavioral assertions. Approve before handing to the developer.

---

### Phase 3: Developer — `@developer`

**Invoke:** `@developer` → provide the path to a story file with Section C tests in RED.

Example: `@developer #file:docs/stories/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Tester Handoff Gate is fully passing
2. Reads the Skeleton Inventory and Test Traceability from Section C, plus all stub and test files
3. Implements production code following dependency order (Domain → Application → Database Schema → API → Adapters)
4. Builds, fixing compilation errors up to 5 times
5. Invokes `review.code.prompt.md` on implemented files — fixes violations up to 5 iterations; if violations
   remain, presents to user for up to 5 user-guided iterations
6. Runs unit tests up to 5 iterations, fixing production code until all unit tests pass (TDD green phase)
7. Runs integration tests up to 5 iterations — fixes production code or updates affected integration tests to
   reflect new contracts, recording each adjustment in the Regression Fixes Log
8. Populates the Developer Handoff Gate in the story file and produces the Developer Handoff Report

**Deliverable:** Implementation files, Section C updated, code review approved, Developer Handoff Report.

**Your role:** Review implementation quality, architecture compliance, and test results. Approve before
moving to final review.

---

### Phase 4: Integration Testing — `@integration.tester`

**Invoke:** `@integration.tester` → provide the path to a story file with completed Developer Handoff Gate.

Example: `@integration.tester #file:docs/stories/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Developer Handoff Gate is fully passing
2. Invokes the `derive-integration-plan` skill to identify cross-layer boundaries, classify existing coverage,
   and detect gaps
3. If all boundaries are already covered, skips to step 5; otherwise presents coverage gaps with a proposed test
   plan — waits for approval before writing tests (up to 3 revision cycles)
4. Creates or modifies integration test files in the `emc.camus.api.integration.test` project following
   `testing.instructions.md` and `testing.integration.instructions.md`
5. Builds integration tests, fixing compilation errors up to 5 iterations
6. Runs integration tests — distinguishes test defects (fixes them up to 5 iterations) from production code
   defects (records as findings with root cause analysis)
7. Populates Section D — Integration Test Traceability, Integration Test Findings, and evaluates the
   Integration Tester Handoff Gate (Status: READY | FAIL | BLOCKED)

**Deliverable:** Integration test files, Section D populated (Traceability + Findings), Integration Test Report.

**Your role:** Review the integration test report. If findings exist, decide whether to fix production code
(using `@code.fix` or `@developer`) and re-run `@integration.tester`, or accept the findings and proceed.
Approve before moving to review.

---

### Phase 5: Review — `@concurrent.reviewer.code` + `@concurrent.reviewer.documentation`

Both steps are **required** before commit. Code changes frequently affect documentation — new adapters need READMEs,
API endpoint changes require architecture and authentication doc updates, and new configuration keys must appear in the
relevant adapter README. Skipping Step 2 causes documentation drift.

**Step 1 — Code review:** `@concurrent.reviewer.code` → check `[branch_name]`.

Example: `@concurrent.reviewer.code check [branch_name]`

`@concurrent.reviewer.code` resolves all branch `.cs` files via `git diff`, matches each file to its instruction
checklists, dispatches three sub-agents (GPT, Opus, Sonnet), and produces a consolidated compliance report.

**Step 2 — Documentation review:** `@concurrent.reviewer.documentation` → check `[branch_name]`.

Example: `@concurrent.reviewer.documentation check [branch_name]`

`@concurrent.reviewer.documentation` resolves all branch `.md` files via `git diff`, evaluates them against the
documentation conventions checklist using three sub-agents, and produces a consolidated compliance report.

**Your role:** Review both reports. For the code review, fix any FAIL findings using `@code.fix` and re-run.
For documentation, fix any FAIL findings using `@documentation.fix` and re-run. Do not proceed until both steps
produce a PASS verdict.

---

### Phase 6: Technical Writer — `@technical_writer`

**Invoke:** `@technical_writer` → provide the path to a story file after both concurrent reviews pass.

Example: `@technical_writer #file:docs/stories/todo/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Integration Tester Handoff Gate is fully passing
2. Reads all context files and the story file — extracts functional requirements, Layer Impact Matrix endpoints,
   and all new or modified production files from the Skeleton Inventory
3. Invokes the `update-changelog` skill with the story file — the skill handles version bump determination
   (MAJOR, MINOR, PATCH, or APPEND), user confirmation, `Directory.Build.props` update, and CHANGELOG entry
   creation grouped under Keep a Changelog subsections (Added, Changed, Fixed, Removed, Security, Deprecated)
4. Updates Swagger/OpenAPI XML annotations for new or modified controller actions (up to 20 endpoints)
5. Updates the Postman collection with new or modified requests
6. Builds to verify changes compile, fixing errors up to 3 times
7. Runs Markdown linting via the `markdown-lint` skill and fixes any errors up to 3 times
8. Populates Section E — evaluates the Technical Writer Handoff Gate and sets status to DOCUMENTED

**Deliverable:** Updated `Directory.Build.props`, `CHANGELOG.md`, Swagger annotations, Postman collection,
Markdown lint passing, Section E populated, Technical Writer Handoff Report.

**Your role:** Review the version bump rationale, CHANGELOG entry accuracy, and Swagger/XML documentation quality.
Approve before finalizing.

---

### Phase 7: QA Tester — `@qa.tester`

**Invoke:** `@qa.tester` → provide the path to a story file after the Technical Writer phase completes.

Example: `@qa.tester #file:docs/stories/todo/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Technical Writer Handoff Gate and all previous handoff gates (Sections A through E) pass
2. Runs the full test suite (`test-all` task) as a sanity gate
3. Collects code coverage (`test-refresh-coverage-report` task) for branch-modified files and identifies gaps
   below 100%
4. Presents coverage gaps with file paths, percentages, and uncovered line numbers — asks you whether to write
   additional unit tests for each
5. Writes approved coverage tests, runs `test-unit`, invokes `@code.fix` on any failures (up to 3 iterations)
6. Runs integration tests (`test-integration` task) as a separate verification step
7. Guides you through local validation: Docker Compose startup (`docker-compose-up-dev-no-api`), API run
   (`run-api`), Postman collection execution, infrastructure teardown (`docker-compose-down`)
8. Populates Section F and the QA Tester Handoff Gate
9. Asks you to confirm everything is ready — on confirmation, moves stories from `todo/` to `done/` via `git mv`

**Deliverable:** Coverage gaps closed (or acknowledged), local validation confirmed, Section F populated,
stories moved to `done/`, QA Tester Handoff Report.

**Your role:** Approve or decline coverage test writing for each gap. Execute the local validation steps
(Docker + Postman) and confirm results. Confirm readiness before stories are moved.

---

### Phase 8: Release Manager — `@release_manager`

**Invoke:** `@release_manager` → provide the path to a story file after the QA Tester phase completes.

Example: `@release_manager #file:docs/stories/todo/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the QA Tester Handoff Gate is fully passing
2. Checks for untracked files that may belong to the feature — presents them for confirmation
3. Commits and pushes the branch (with your confirmation)
4. Creates a PR to `main` via `gh pr create` with story-derived title and acceptance criteria in body
5. Evaluates whether a release is needed (version in `Directory.Build.props` ≠ latest git tag)
6. If release needed and you confirm: creates a GitHub release with CHANGELOG-derived notes
7. Creates deployment PRs from `main` → `deploy/dev` and `main` → `deploy/prod`

**Deliverable:** Main PR created, release created (if applicable), deployment PRs created, Release Manager
Handoff Report (Status: DONE | BLOCKED).

**Your role:** Confirm before each push, PR creation, and release creation.

---

## Quick Reference

| Phase | Agent | Input | Output |
| ----- | ----- | ----- | ------ |
| 0 | `@product_owner` | Feature request (free text) | `feat_` branch + story files in `todo/` with Section A |
| 1 | `@architect` | Story file (Section A complete) | Section B populated |
| 2 | `@unit.tester` | Story file (Sections A + B complete) | Stub files + test files + Section C |
| 3 | `@developer` | Story file (Section C tests in RED) | Implementation + code review approved |
| 4 | `@integration.tester` | Story file (Developer Handoff Gate complete) | Integration tests + Section D + status report |
| 5a | `@concurrent.reviewer.code` | branch name | Consolidated code compliance report (multi-model) |
| 5b | `@concurrent.reviewer.documentation` | branch name | Consolidated documentation compliance report (multi-model) |
| 6 | `@technical_writer` | Story file (Review phase PASS) | Version bump + CHANGELOG + Swagger + Postman + XML docs + Section E |
| 7 | `@qa.tester` | Story file (Technical Writer Gate PASS) | Coverage closed + local validation + stories moved + Section F |
| 8 | `@release_manager` | Story file (QA Tester Gate PASS) | Main PR + release + deploy PRs (Status: DONE \| BLOCKED) |

## Tips

- **Start a new chat session** for each agent invocation to keep context clean.
- **Reference files** with `#file:path` syntax in Copilot Chat for precise context.
- **Section C is the TDD tracker** — the tester populates it (Skeleton Inventory + Test Traceability), the developer
  updates it, the code reviewer verifies against it.
- **Section D is the integration test tracker** — the integration tester populates it (Integration Test Traceability +
  Findings), with human approval before writing any tests.
- **Section E is the documentation tracker** — the technical writer populates it via the `update-changelog` skill
  (Version Update, CHANGELOG Entry, Documentation Updates), delegating version bumps and changelog management.
- **Section F is the QA tracker** — the QA tester populates it (Test Suite, Coverage, Local Validation, Stories
  Moved), confirming quality and moving stories to done before release.
- **`@release_manager` does not update the story file** — it only reports results (PRs, release, deployments)
  in its handoff report output.
- **Phase 5 is mandatory** — both `@concurrent.reviewer.code` and `@concurrent.reviewer.documentation` must
  produce PASS before the technical writer can proceed. Run them sequentially: code review first, documentation
  review second.
- **`@documentation.fix` handles the fix loop automatically** — it invokes `review.documentation.prompt.md`
  internally and iterates until the reviewer returns PASS or the iteration limit (5) is reached. Use it to fix any
  documentation FAIL findings from Phase 5.
- **Use `@code.fix`** for ad-hoc code fixes outside the story workflow (bugs, tech debt).
- **Use `@documentation.fix`** standalone to fix any documentation scope — file, directory, layer, or `uncommitted`.
- **Use `@concurrent.reviewer.code`** standalone to review any `.cs` scope — file, directory, layer, or `uncommitted`.
- **Use `@concurrent.reviewer.documentation`** standalone to validate docs without fixing (read-only review).
- **Use `@concurrent.reviewer.copilot.customization`** to review agent/prompt/instruction/skill files.
- **Create the `feat_` branch first** — `@product_owner` creates the branch automatically from latest `main`
  and confirms the name with you before proceeding.
- **Stories live in `todo/` during development** — agents create and update stories under `docs/stories/todo/`.
- **Move stories to `done/` via `@qa.tester`** — the QA tester moves stories after local validation passes,
  before handing off to the release manager.
- **The story file is the single source of truth** — all agents reference and update it.
- **Any agent can run standalone** — useful for reviewing existing code outside the full workflow.
- **`@concurrent.reviewer.copilot.customization`** maintains SDLC quality — run it when modifying agents,
  prompts, instructions, or skills, not during feature development.
