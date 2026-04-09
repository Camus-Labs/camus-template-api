# Architecture

## Overview

Camus follows **Hexagonal Architecture (Ports & Adapters)** pattern with clean separation of concerns. Dependencies
point inward: outer layers depend on inner layers, never the reverse.

## Layer Structure

```text
┌─────────────────────────────────────────────┐
│           Adapters (External)               │
│  API Controllers | PostgreSQL | Dapr        │
├─────────────────────────────────────────────┤
│         Application Layer                   │
│  Use Cases | Service Interfaces             │
├─────────────────────────────────────────────┤
│         Domain Layer (Core)                 │
│  Business Logic | Domain Models             │
└─────────────────────────────────────────────┘
```

## Layers

### Domain (`emc.camus.domain`)

Innermost layer containing business entities, invariants, and rules. Has zero external dependencies.

> **📖 Full Reference:** See [Domain Layer README](../src/Domain/emc.camus.domain/README.md) for entity
documentation and namespace structure.

### Application (`emc.camus.application`)

Shared contracts layer defining port interfaces, attributes, exceptions, and constants consumed by the API layer
and infrastructure adapters. Also contains concrete application services that orchestrate domain operations.

> **📖 Full Reference:** See [Application Layer README](../src/Application/emc.camus.application/README.md) for
complete contracts documentation and namespace structure.

### Adapters

Infrastructure implementations that fulfill Application-layer port interfaces. Each adapter is independently
swappable:

- **API** (`emc.camus.api`) — REST endpoints, middleware, and composition root
- **Persistence** (`emc.camus.persistence.postgresql`) — PostgreSQL database access with Dapper
- **Observability** (`emc.camus.observability.otel`) — OpenTelemetry tracing, metrics, and structured logging
- **Rate Limiting** (`emc.camus.ratelimiting.inmemory`) — IP-based sliding window rate limiter
- **Secrets** (`emc.camus.secrets.dapr`) — Dapr secret management
- **Security JWT** (`emc.camus.security.jwt`) — JWT Bearer authentication
- **Security API Key** (`emc.camus.security.apikey`) — API Key authentication
- **Documentation** (`emc.camus.documentation.swagger`) — Swagger/OpenAPI documentation

Each adapter README contains configuration, integration, and troubleshooting details.

## Dependency Flow

```text
API → Application → Domain
     ↓
  Adapters (PostgreSQL, Observability)
```

**Rule:** Dependencies point inward. Domain has zero external dependencies. Adapters depend on Application
interfaces but never on each other.

## Cross-Cutting Concerns

Observability, security, rate limiting, and request timeouts are implemented as cross-cutting concerns wired
through the API composition root. See the following guides for architectural and configuration details:

- **Authentication** — [Authentication Guide](authentication.md) covers JWT and API Key mechanisms
- **Observability** — [Observability Adapter](../src/Adapters/emc.camus.observability.otel/README.md) and
  [Infrastructure Configuration](../src/Infrastructure/observability/README.md) cover the telemetry pipeline
- **Request Timeouts** — Configured in the API layer via ASP.NET Core built-in request timeouts with named
  policies (default, tight, extended) and appsettings-driven durations
