# Architecture

## Overview

Camus follows **Hexagonal Architecture (Ports & Adapters)** pattern with clean separation of concerns. Dependencies
point inward: outer layers depend on inner layers, never the reverse.

## Layer Structure

```text
┌─────────────────────────────────────────────┐
│         API Layer (Outermost)               │
│  Controllers | Middleware | Auth | Swagger  │
├─────────────────────────────────────────────┤
│         Adapters (External)                 │
│  PostgreSQL | Dapr | OpenTelemetry          │
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

- **Persistence** (`emc.camus.persistence.postgresql`) — PostgreSQL database access with Dapper
- **Observability** (`emc.camus.observability.otel`) — OpenTelemetry tracing, metrics, and structured logging
- **Secrets** (`emc.camus.secrets.dapr`) — Dapr secret management
- **Cache** (`emc.camus.cache.inmemory`) — In-memory token revocation and idempotency response caching
- **Migrations** (`emc.camus.migrations.dbup`) — Database schema versioning with DbUp
- **Persistence In-Memory** (`emc.camus.persistence.inmemory`) — In-memory repositories for development and testing

### API (`emc.camus.api`)

Outermost layer hosting the HTTP pipeline, controllers, middleware, and cross-cutting features tightly coupled
to the hosting infrastructure. Implements authentication, rate limiting, idempotency, error handling, security
headers, API versioning, request timeouts, health checks, and API documentation directly — since these are
HTTP-pipeline concerns rather than swappable infrastructure adapters.

> **📖 Full Reference:** See [API Layer README](../src/Api/emc.camus.api/README.md) for configuration,
middleware pipeline, and extension documentation.

Each adapter README contains configuration, integration, and troubleshooting details. The
[API Layer README](../src/Api/emc.camus.api/README.md) covers the HTTP-pipeline features (authentication,
rate limiting, Swagger, etc.) that live directly in the API project.

## Dependency Flow

```text
API       → Application → Domain
Adapters  → Application
```

**Rule:** Dependencies point inward. Domain has zero external dependencies. Adapters depend on Application
interfaces but never on each other.
