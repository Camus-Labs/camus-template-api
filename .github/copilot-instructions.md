# Copilot Instructions

## Project

.NET 9.0 REST API — solution at `src/CamusApp.sln`.

## Architecture

Hexagonal Architecture (Ports & Adapters). Dependencies point inward:
`API/Adapters → Application → Domain`.

- **Domain** (`src/Domain/`) — Business entities, rules, value objects. Zero external dependencies.
- **Application** (`src/Application/`) — Contracts (interfaces, CQRS types, attributes,
  exceptions, constants) and concrete application services. No infrastructure implementations.
- **API** (`src/Api/`) — Controllers, middleware, DI, HTTP pipeline.
- **Adapters** (`src/Adapters/`) — Implement Application interfaces. Each independently swappable.
- **Tests** (`src/Test/`) — xUnit + FluentAssertions. Unit test projects (`.test` suffix) per adapter/layer;
  single integration test project (`.integration.test` suffix).

Full architecture reference: `docs/architecture.md`

## File Editing Hard Rules

- NEVER modify workspace files through terminal text-processing tools (`sed`, `awk`, `perl -i`, `tr`, here-docs that
  write to tracked files, or `python -c '... open(... "w")'`). These bypass the editor's diff view and apply changes
  without user approval.
- ALL file edits MUST go through editor tools (`create_file`, `replace_string_in_file`, `multi_replace_string_in_file`,
  `edit_notebook_file`) so the user sees the diff and approves the change.
- The terminal is only for read-only commands, builds, tests, git, linters, and similar non-mutating operations on
  workspace files.
- If a bulk edit is needed, perform it as multiple `multi_replace_string_in_file` operations rather than one `sed`
  invocation, even if it requires more calls.
