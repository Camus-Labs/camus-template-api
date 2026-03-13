# Feature Development Workflow

Agent-driven workflow for implementing features in the Camus API. Each phase produces a deliverable that feeds
the next, with human approval gates between phases. Agents are invoked with `@name` in Copilot Chat.

## Workflow Overview

``` text
┌──────────────────────────────────────────────────────────────────────────┐
│                        FEATURE DEVELOPMENT PIPELINE                      │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  PRE: CREATE BRANCH ──────── User action                                 │
│  │  Action: git checkout main && git pull && git checkout -b feat_...     │
│  │  Gate:   User confirms branch created from latest main                │
│  ▼                                                                       │
│  Phase 0: PRODUCT OWNER ──── @product_owner                              │
│  │  Input:  Feature request (free text)                                  │
│  │  Output: docs/stories/todo/{slug}/US-*.md (Section A)                 │
│  │  Gate:   Human reviews & approves stories                             │
│  ▼                                                                       │
│  Phase 1: ARCHITECT ──────── @architect                                  │
│  │  Input:  Story file with completed Section A                          │
│  │  Output: Section B populated (architecture + implementation plan)     │
│  │  Gate:   Human reviews & approves architecture                        │
│  ▼                                                                       │
│  Phase 2: TESTER ─────────── @tester                                     │
│  │  Input:  Story file with completed Sections A + B                     │
│  │  Output: Stubs + test files (TDD red) + Section C                     │
│  │  Gate:   Human reviews test design + production skeleton              │
│  ▼                                                                       │
│  Phase 3: DEVELOPER ──────── @developer                                  │
│  │  Input:  Story file with Section C tests in RED                       │
│  │  Output: Implementation (TDD green) + code review approved            │
│  │  Sub:    @reviewer.code (multi-model, invoked automatically)          │
│  │  Gate:   Human reviews implementation                                 │
│  ▼                                                                       │
│  Phase 4: REVIEW ─────────── @reviewer.code + @documentation.fix        │
│  │  Input:  Uncommitted files from previous phases                       │
│  │  Step 1: @reviewer.code uncommitted (final code compliance check)     │
│  │  Step 2: @documentation.fix uncommitted (fix docs until compliant)    │
│  │  Gate:   Human reviews reports, approves final quality                │
│  ▼                                                                       │
│  POST: COMPLETE & COMMIT ─── User action                                 │
│  │  Action: Move stories to done/, bump version, update changelog        │
│  │  Gate:   User confirms all updated and committed                      │
│  ▼                                                                       │
│  DONE ✓                                                                  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Phases in Detail

### Pre: Create Branch — User Action

Before any agent work begins, create a feature branch from the latest `main`.

**Steps:**

1. `git checkout main && git pull`
2. `git checkout -b feat_<short-description>`

**Your role:** Confirm the branch is created from the latest `main` and follows the `feat_` naming convention.

---

### Phase 0: Product Owner — `@product_owner`

**Invoke:** `@product_owner` → describe the feature request in free text.

Example: `@product_owner I need CRUD operations for managing user profiles with email validation`

**What the agent does:**

1. Decomposes the feature request into atomic user stories
2. Creates story files under `docs/stories/todo/{request-slug}/` using the template
3. Populates Section A (Product Owner Definition) — story statement, scope, FRs, NFRs, ACs
4. Asks up to 5 rounds of clarification questions for ambiguous requirements
5. Evaluates the Product Owner Handoff Gate per story

**Deliverable:** Story files with completed Section A, handoff gate evaluated, status set.

**Your role:** Review stories. Verify scope boundaries, acceptance criteria completeness,
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

**Deliverable:** Story file with completed Section B, implementation plan ordered by
dependency direction, and status set to `READY_FOR_IMPLEMENTATION`.

**Your role:** Review architecture. Verify layer boundaries, implementation plan order,
and cross-cutting decisions. Approve before proceeding to testing.

---

### Phase 2: Tester — `@tester`

**Invoke:** `@tester` → provide the path to a story file with completed Sections A and B.

Example: `@tester #file:docs/stories/user-profiles/US-01-create-profile.md`

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

**Your role:** Review the production skeleton and test design. Verify coverage of every AC, proper
naming, behavioral assertions. Approve before handing to the developer.

---

### Phase 3: Developer — `@developer`

**Invoke:** `@developer` → provide the path to a story file with Section C tests in RED.

Example: `@developer #file:docs/stories/user-profiles/US-01-create-profile.md`

**What the agent does:**

1. Validates the Tester Handoff Gate is fully passing
2. Reads the Skeleton Inventory and Test Traceability from Section C, plus all stub and test files
3. Implements production code following dependency order (Domain → Application → Database Schema → API → Adapters)
4. Builds, fixing compilation errors up to 5 times
5. Invokes `@reviewer.code` on implemented files — fixes violations up to 5 iterations; if violations remain, reports
   to user for up to 5 user-guided iterations
6. Runs tests up to 5 iterations, fixing production code until all tests pass (TDD green phase)
7. Populates the Developer Handoff Gate in the story file
8. Evaluates the Developer Handoff Gate

**Deliverable:** Implementation files, Section C updated, code review approved, Developer Handoff Report.

**Your role:** Review implementation quality, architecture compliance, and test results.
Approve before moving to final review.

---

### Phase 4: Review — `@reviewer.code` + `@documentation.fix`

Both steps are **required** before commit. Code changes frequently affect documentation — new adapters need READMEs,
API endpoint changes require architecture and authentication doc updates, and new configuration keys must appear in the
relevant adapter README. Skipping Step 2 causes documentation drift.

**Step 1 — Code review:** `@reviewer.code` → pass the keyword `uncommitted`.

Example: `@reviewer.code uncommitted`

`@reviewer.code` resolves all uncommitted `.cs` files via `git diff`, matches each file to its instruction checklists,
dispatches three sub-agents (Codex, Opus, Sonnet), and produces a consolidated compliance report.

**Step 2 — Documentation fix:** `@documentation.fix` → pass the keyword `uncommitted`.

Example: `@documentation.fix uncommitted`

`@documentation.fix` resolves all uncommitted `.md` files via `git diff`, evaluates them against the documentation
conventions checklist, applies fixes, then invokes `@reviewer.documentation` to verify compliance — iterating up to
5 automatic fix cycles plus 5 user-guided cycles until the reviewer returns PASS. Run this step even when no `.md`
files were explicitly changed — the agent detects whether new or modified code requires documentation updates
to adapter READMEs, `docs/architecture.md`, or `docs/authentication.md`.

**Your role:** Review both reports. For the code review, fix any FAIL findings using `@code.fix` and re-run.
For documentation, `@documentation.fix` handles fixes automatically — review its final report and approve when
both show PASS. Do not merge until both steps produce a PASS verdict.

---

### Post: Complete & Commit — User Action

After Phase 4 produces PASS for both code and documentation reviews, finalize the feature.

**Steps:**

1. Move the story folder from `docs/stories/todo/{request-slug}/` to `docs/stories/done/{request-slug}/`
2. Bump the version in `src/Directory.Build.props` and add a matching `CHANGELOG.md` entry
   (see [Contributing — Versioning Standard](../CONTRIBUTING.md#versioning-standard))
3. Commit all changes to the feature branch

**Your role:** Confirm stories are moved to `done/`, version is bumped, changelog is updated,
all changes are committed, and the feature branch is ready for merge. Use the story details as the PR request
details.

---

## Quick Reference

| Phase | Agent | Input | Output |
| ----- | ----- | ----- | ------ |
| Pre | User | Latest `main` branch | `feat_` branch created |
| 0 | `@product_owner` | Feature request (free text) | Story files in `todo/` with Section A |
| 1 | `@architect` | Story file (Section A complete) | Section B populated |
| 2 | `@tester` | Story file (Sections A + B complete) | Stub files + test files + Section C |
| 3 | `@developer` | Story file (Section C tests in RED) | Implementation + code review approved |
| 4a | `@reviewer.code` | `uncommitted` | Consolidated code compliance report (multi-model) |
| 4b | `@documentation.fix` | `uncommitted`, file, or directory | Documentation fixed + compliance report |
| Post | User | All phases PASS | Stories moved to `done/`, changes committed |

## Tips

- **Start a new chat session** for each agent invocation to keep context clean.
- **Reference files** with `#file:path` syntax in Copilot Chat for precise context.
- **Section C is the TDD tracker** — the tester populates it (Skeleton Inventory + Test Traceability), the developer
  updates it, the code reviewer verifies against it.
- **Phase 4 is mandatory** — both `@reviewer.code uncommitted` and `@documentation.fix uncommitted` must produce
  PASS before merging. Run them sequentially: code review first, documentation fix second.
- **`@documentation.fix` handles the fix loop automatically** — it invokes `@reviewer.documentation` internally and
  iterates until the reviewer returns PASS or the iteration limit is reached. Run it even when you did not edit `.md`
  files — code changes often require documentation updates.
- **Use `@code.fix`** for ad-hoc code fixes outside the story workflow (bugs, tech debt).
- **Use `@documentation.fix`** standalone to fix any documentation scope — file, directory, layer, or `uncommitted`.
- **Use `@reviewer.code`** standalone to review any `.cs` scope — file, directory, layer, or `uncommitted`.
- **Use `@reviewer.documentation`** standalone to validate docs without fixing (read-only review).
- **Create the `feat_` branch first** — all agent work happens on a feature branch, never directly on `main`.
- **Stories live in `todo/` during development** — agents create and update stories under `docs/stories/todo/`.
- **Move stories to `done/` after final review** — once Phase 4 passes, move the story folder from `todo/` to
  `done/` and commit.
- **The story file is the single source of truth** — all agents reference and update it.
- **Any agent can run standalone** — useful for reviewing existing code outside the full workflow.
- **Meta-reviewers** (`@reviewer.agents`, `@reviewer.prompts`) maintain SDLC quality — run them when modifying agents
  or prompts, not during feature development.
