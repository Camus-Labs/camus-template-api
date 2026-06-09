# Contributing to Camus API

Thank you for contributing to the Camus API. This guide covers the rules and conventions every
contributor must follow.

---

## Branching Convention

This project follows a **release-centric** branching model. The full workflow is described in
[`docs/agentic-sdlc-workflow.md`](docs/agentic-sdlc-workflow.md); the short version:

```text
main
└── release/next        (work-in-progress release; cut from main by @product_owner)
    │                    (renamed to release/v<X.Y.Z> by @technical_writer at release time)
    └── feat/<slug>      (one per feature; cut from the release branch)
```

- `release/next` is the active release branch during development; `@technical_writer` renames it (and the
  matching `docs/stories/next/` folder) to `release/v<X.Y.Z>` once the version is decided.
- `feat/<slug>` branches are always created from and merged back into `release/next` via
  **squash + delete branch**. Feature work never targets `release/v<X.Y.Z>`; by the time the release branch
  has been renamed, no new feature merges are accepted into that release.
- `release/v<X.Y.Z>` is merged into `main` via **rebase + keep branch**, then tagged on `main`.
- Never commit manually to `main`, `release/next`, or any `release/v*` branch. Release-scope commits to
  `release/next` and `release/v*` happen only through the agentic SDLC skills
  (`commit-and-push-on-release-branch`, `complete-feature`, `apply-release-version`).

Branch names and PR targets are enforced by the **Validate PR Branches** workflow
(`.github/workflows/validate-pr-branches.yml`).

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

For new feature development, this project uses an **agent-driven SDLC workflow** with phased handoffs and human
approval gates. See [Feature Development Workflow](docs/agentic-sdlc-workflow.md) for the complete process.

The workflow covers: requirements decomposition, architecture definition, TDD test creation, implementation,
automated code review, and documentation compliance — all coordinated through specialized agents.

---

## Testing

See [Test Projects](src/Test/README.md) for frameworks, conventions, and coverage targets.

---

## Security

Report security vulnerabilities per [SECURITY.md](SECURITY.md). Never commit secrets, keys, or connection
strings to the repository.
