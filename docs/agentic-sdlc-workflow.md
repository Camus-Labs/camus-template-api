# Feature Development Workflow

Agent-driven workflow for implementing features in the Camus API. Each phase produces a deliverable that feeds
the next, with human approval gates between phases. Agents are invoked with `@name` in Copilot Chat.

## Workflow Overview

``` text
┌──────────────────────────────────────────────────────────────────────────┐
│                        FEATURE DEVELOPMENT PIPELINE                      │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Phase 1: ARCHITECT ──────── @architect                                  │
│  │  Input:  Feature requirement (free text)                              │
│  │  Output: docs/features/{name}.md (stories + file plan + diagram)      │
│  │  Gate:   Human reviews & approves plan                                │
│  ▼                                                                       │
│  ┌──── Per Story (TDD Cycle) ─────────────────────────────────────┐      │
│  │                                                                 │      │
│  │  Phase 2a: TESTER ────────── @tester                            │      │
│  │  │  Input:  Feature doc + story number                          │      │
│  │  │  Output: Unit test files (red phase — tests fail)            │      │
│  │  │  Gate:   Human reviews test design                           │      │
│  │  ▼                                                              │      │
│  │  Phase 2b: TEST REVIEW ───── @reviewer.tests                    │      │
│  │  │  Input:  Feature doc + story number                          │      │
│  │  │  Output: Test quality verdict (PASS / PASS WITH NOTES / FAIL)│      │
│  │  │  Gate:   Human reviews verdict                               │      │
│  │  ▼                                                              │      │
│  │  Phase 3: DEVELOPER ──────── @developer                         │      │
│  │  │  Input:  Feature doc + story number + failing tests          │      │
│  │  │  Output: Implementation code (green phase — tests pass)      │      │
│  │  │  Gate:   Human reviews implementation                        │      │
│  │                                                                 │      │
│  └──── Repeat for each story ─────────────────────────────────────┘      │
│  ▼                                                                       │
│  Phase 4: REVIEW ──────────── Run independently / concurrently           │
│  │                                                                       │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │  │ @reviewer.  │ │ @reviewer.   │ │ @reviewer.  │ │ @reviewer.   │ │
│  │  │ domain      │ │ application  │ │ api         │ │ adapters     │ │
│  │  └──────┬──────┘ └──────┬───────┘ └──────┬──────┘ └──────┬───────┘ │
│  │  ┌──────┴──────┐ ┌──────┴────────┐ ┌─────┴────────┐                │
│  │  │ @reviewer.  │ │ @reviewer.    │ │ @reviewer.   │                │
│  │  │ cross-      │ │ observability │ │ documentation│                │
│  │  │ cutting     │ │               │ │              │                │
│  │  └──────┬──────┘ └──────┬────────┘ └──────┬───────┘                │
│  │         └───────────┬───┴─────────────────┘                        │
│  │                     ▼                                              │
│  │          @reviewer.report                                          │
│  │  Gate:   Human reviews consolidated report                            │
│  ▼                                                                       │
│  Phase 5: FIX ────────────── @developer (reuse)                          │
│  │  Input:  Review report violations                                     │
│  │  Output: Fixed code                                                   │
│  │  Gate:   Re-run affected reviewers until clean                        │
│  ▼                                                                       │
│  DONE ✓                                                                  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Phases in Detail

### Phase 1: Architect — `@architect`

**Invoke:** Open Copilot Chat → type `@architect` → describe the feature requirement.

**What the agent does:**

1. Analyzes the feature requirement against the current codebase
2. Identifies which layers are affected (Domain, Application, API, Adapters)
3. Creates a **File Plan** — tables per layer with Action (Create/Modify/Remove), Path, and Purpose
4. Creates a **Mermaid Object Diagram** showing entities, services, and their relationships
5. Breaks the feature into atomic, well-defined user stories with file references
6. Creates `docs/features/{feature-name}.md` from the template

**Deliverable:** Feature plan document with file plan, object diagram, numbered user stories,
each containing acceptance criteria, affected layers, files from file plan, and dependencies.

**Your role:** Review the plan. Verify the file plan follows naming conventions, stories are
atomic, correctly layered, and properly ordered. Approve or request changes before proceeding.

---

### Phase 2a: Tester — `@tester`

**Invoke:** `@tester` → reference the feature doc and specify the story number.

Example: `@tester Implement tests for Story 1 from #file:docs/features/user-management.md`

**What the agent does:**

1. Reads the specific story's acceptance criteria, file plan, and object diagram
2. Identifies the correct test project based on affected layers
3. Creates test classes following project conventions:
   - xUnit + FluentAssertions + Moq
   - `MethodName_Scenario_ExpectedResult` naming
   - AAA pattern with comments
   - Wildcard exception assertions
4. Creates minimal stubs so tests compile but **fail** (TDD red phase)
5. Runs `dotnet build` to verify compilation

**Deliverable:** Test files in the appropriate `src/Test/` project(s). Report listing
all test methods created and what each validates.

**Your role:** Review test design. Verify coverage of acceptance criteria, proper naming,
and that tests are behavioral (not implementation-detail tests).

---

### Phase 2b: Test Review — `@reviewer.tests`

**Invoke:** `@reviewer.tests` → reference the feature doc and specify the story number.

Example: `@reviewer.tests Review tests for Story 1 from #file:docs/features/user-management.md`

**What the agent does:**

1. Reads all test files for the story
2. Validates naming conventions, assertion patterns, mocking strategy
3. Checks test coverage against acceptance criteria
4. Produces a verdict: **PASS**, **PASS WITH NOTES**, or **FAIL**

**Deliverable:** Test review report with verdict and any issues found.

**Your role:** Review verdict. If FAIL, go back to `@tester` for fixes. If PASS, proceed to development.

---

### Phase 3: Developer — `@developer`

**Invoke:** `@developer` → reference the feature doc and specify the story number.

Example: `@developer Implement Story 1 from #file:docs/features/user-management.md`

**What the agent does:**

1. Reads the story, file plan, object diagram, and existing failing tests
2. Implements production code following all conventions:
   - Hexagonal architecture boundaries
   - Entity patterns (private set, Reconstitute, business methods)
   - CQRS types (Commands, Results, Filters, Views)
   - Controller patterns (ApiControllerBase, activity source wrapping)
   - DTO separation, versioned mapping extensions
   - Argument validation on all public members
3. Runs tests to verify they pass (TDD green phase)
4. Updates the feature doc progress table

**Deliverable:** Implementation files across affected layers. Report listing all files
created/modified and what each does.

**Your role:** Review implementation quality, architecture compliance, and test results.
Approve before moving to the next story or review phase.

---

### Phase 4: Review — Layer-Specific Agents

Run one, several, or all review agents. Each is independent and can run in separate
Copilot Chat sessions concurrently.

| Agent | Scope | Focus |
| ----- | ----- | ----- |
| `@reviewer.domain` | Domain layer | Entity pattern, purity, value objects, business rules |
| `@reviewer.application` | Application layer | CQRS types, services, contracts, DON'T violations |
| `@reviewer.api` | API layer | Controllers, DTOs, mapping, versioning |
| `@reviewer.adapters` | Adapter layer | Implementations, repositories, settings, DI |
| `@reviewer.cross-cutting` | All layers | Validation flow, constants, interfaces, hexagonal compliance |
| `@reviewer.observability` | All layers | Tracing, metrics, logging |
| `@reviewer.documentation` | All layers | XML docs, SSOT, accuracy, Swagger |

**Invoke:** `@reviewer.domain` → specify the feature name.

Example: `@reviewer.domain Review the user-management feature`

**Each agent:**

1. Finds all files related to the feature within its scope
2. Checks against its specific section of `docs/review-checklist.md`
3. Reports only definite violations (not suggestions)
4. Outputs structured report with file:line references

**Consolidation:** After running the reviewers you want, use `@reviewer.report`
and paste or reference the individual reports to get a single prioritized action list.

**Your role:** Review each report. Decide which violations to fix now vs. defer.

---

### Phase 5: Fix — Reuse `@developer`

**Invoke:** `@developer` → paste the review violations to fix.

Example: `@developer Fix the following review violations: [paste report]`

The developer agent will apply fixes and re-run tests. After fixing, re-run
the affected reviewer(s) to verify compliance.

---

## Quick Reference

| Phase | Agent | Input | Output |
| ----- | ----- | ----- | ------ |
| 1 | `@architect` | Feature requirement text | `docs/features/{name}.md` |
| 2a | `@tester` | Feature doc + story # | Test files in `src/Test/` |
| 2b | `@reviewer.tests` | Feature doc + story # | Test quality verdict |
| 3 | `@developer` | Feature doc + story # | Implementation files |
| 4 | `@reviewer.domain` | Feature name | Domain violations report |
| 4 | `@reviewer.application` | Feature name | Application violations report |
| 4 | `@reviewer.api` | Feature name | API violations report |
| 4 | `@reviewer.adapters` | Feature name | Adapter violations report |
| 4 | `@reviewer.cross-cutting` | Feature name | Cross-cutting violations report |
| 4 | `@reviewer.observability` | Feature name | Observability violations report |
| 4 | `@reviewer.documentation` | Feature name | Documentation violations report |
| 4 | `@reviewer.report` | Individual reports | Consolidated action list |
| 5 | `@developer` | Review violations | Fixed code |

## File Structure

``` text
.github/
  copilot-instructions.md               ← Global rules (already exists)
  agents/
    architect.agent.md                   ← Phase 1: Feature → Stories + File Plan + Diagram
    tester.agent.md                      ← Phase 2a: Story → Tests (TDD red)
    reviewer.tests.agent.md              ← Phase 2b: Test quality gate
    developer.agent.md                   ← Phase 3: Story → Implementation (TDD green)
    reviewer.domain.agent.md             ← Phase 4: Domain layer review
    reviewer.application.agent.md        ← Phase 4: Application layer review
    reviewer.api.agent.md                ← Phase 4: API layer review
    reviewer.adapters.agent.md           ← Phase 4: Adapter layer review
    reviewer.cross-cutting.agent.md      ← Phase 4: Cross-cutting concerns review
    reviewer.observability.agent.md      ← Phase 4: Observability review
    reviewer.documentation.agent.md      ← Phase 4: Documentation review
    reviewer.report.agent.md             ← Phase 4: Consolidate reports
  instructions/
    domain.instructions.md               ← Auto-attaches for src/Domain/**
    application.instructions.md          ← Auto-attaches for src/Application/**
    api.instructions.md                  ← Auto-attaches for src/Api/**
    adapters.instructions.md             ← Auto-attaches for src/Adapters/**
    testing.instructions.md              ← Auto-attaches for src/Test/**
docs/
  features/
    _template.md                         ← Feature plan template
    {feature-name}.md                    ← Created per feature
  workflow.md                            ← This file
```

## Tips

- **Start a new chat session** for each agent invocation to keep context clean.
- **Reference files** with `#file:path` syntax in Copilot Chat for precise context.
- **Run reviews concurrently** by opening multiple chat sessions, one per reviewer.
- **Iterate TDD per story**: Phases 2a + 2b + 3 repeat for each story before moving to review.
- **Re-run reviewers** after fixes to verify compliance — they are idempotent.
- **Feature doc is the single source of truth** — all agents reference it.
- **Any agent can run standalone** — useful for reviewing existing code outside the full workflow.
