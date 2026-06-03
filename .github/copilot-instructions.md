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
