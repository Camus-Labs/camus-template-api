# Contributing to Camus API

Thank you for contributing to the Camus API Template. This guide covers the rules and conventions
every contributor must follow.

---

## Branching Convention

Create feature/fix branches from the latest `main` using the `feat_` or `fix_` prefix:

1. `git checkout main && git pull`
2. `git checkout -b feat_<short-description>`

All development work happens on feature/fix branches — never commit directly to `main`.

---

## Versioning Standard

This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The version is maintained
in two locations that must stay in sync:

| File | Format | Role |
| ---- | ------ | ---- |
| `src/Directory.Build.props` | `<Version>X.X.X</Version>` | Canonical version — applies to all assemblies |
| [`CHANGELOG.md`](CHANGELOG.md) | `## [X.X.X] - YYYY-MM-DD` | Release history — must have a matching entry |

### When to Bump the Version

Increment the version on every pull request to `main`:

- **Patch** (X.X.**X**) — Bug fixes, minor documentation corrections
- **Minor** (X.**X**.0) — New features, new adapters, new endpoints
- **Major** (**X**.0.0) — Breaking API changes, architectural restructuring

### How to Bump the Version

1. Update `<Version>` in `src/Directory.Build.props`
2. Add a new `## [X.X.X] - YYYY-MM-DD` section in [`CHANGELOG.md`](CHANGELOG.md) above latest release
3. Record completed items into the new version section

### CI Enforcement

The **Build Version Check** workflow (`.github/workflows/build-version-check.yml`) runs on every pull request to
`main`. It validates:

- `src/Directory.Build.props` version matches the latest `## [X.X.X]` entry in [`CHANGELOG.md`](CHANGELOG.md)
- Pull requests have a version bump compared to `main`

The workflow will block merging if either check fails.

---

## Changelog Format

Follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions:

- Group changes under `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed`, `### Security`
- Write entries as imperative statements describing what changed
- When releasing, create a versioned section with the date: `## [X.X.X] - YYYY-MM-DD`

---

## Pull Request Requirements

Before submitting a pull request:

1. **Tests pass** — Run `dotnet test src/CamusApp.sln` and confirm all tests pass
2. **Version bumped** — Update `src/Directory.Build.props` and [`CHANGELOG.md`](CHANGELOG.md)
   (see [Versioning Standard](#versioning-standard))
3. **Documentation updated** — Update relevant documentation for any behavioral changes
4. **Code reviewed** — Ensure code follows the project conventions

Use the story details or feature description as the PR description.

---

## Development Workflow

For new feature development, this project uses an **agent-driven SDLC workflow** with phased handoffs
and human approval gates. See [Feature Development Workflow](docs/agentic-sdlc-workflow.md) for the
complete process.

The workflow covers: requirements decomposition, architecture definition, TDD test creation,
implementation, automated code review, and documentation compliance — all coordinated through
specialized agents.

---

## Testing

See [Test Projects](src/Test/README.md) for frameworks, conventions, and coverage targets.

---

## Security

Report security vulnerabilities per [SECURITY.md](SECURITY.md). Never commit secrets, keys, or
connection strings to the repository.
