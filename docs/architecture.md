# Architecture

## Overview

Camus follows **Hexagonal Architecture (Ports & Adapters)** pattern with clean separation of concerns.

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

## Key Components

### Domain (`emc.camus.domain`)

- Business entities and rules
- Independent of infrastructure
- Contains authentication models and contracts

### Application (`emc.camus.application`)

- Use cases and application services
- Orchestrates domain logic
- Defines port interfaces

### Adapters

- **API** (`emc.camus.api`) - REST endpoints, middleware
- **Persistence** (`emc.camus.persistence.postgresql`) - Database access with Dapper
- **Observability** (`emc.camus.observability.otel`) - OpenTelemetry integration
- **Secrets** (`emc.camus.secrets.dapr`) - Dapr secret management
- **Security JWT** (`emc.camus.security.jwt`) - JWT Bearer authentication
- **Security API Key** (`emc.camus.security.apikey`) - API Key authentication
- **Documentation** (`emc.camus.documentation.swagger`) - Swagger/OpenAPI documentation

## Dependency Flow

```text
API → Application → Domain
     ↓
  Adapters (PostgreSQL, Observability)
```

**Rule:** Dependencies point inward. Domain has zero external dependencies.

## Observability Stack

- **OpenTelemetry Collector** - Centralized telemetry pipeline
- **Jaeger** - Distributed tracing
- **Prometheus** - Metrics collection
- **Grafana** - Visualization dashboards
- **Loki** - Log aggregation

## Security Architecture

- **JWT Authentication** - RSA256 token validation
- **API Key Authentication** - Header-based (`X-Api-Key`)
- **CORS** - Configurable policies
- **Rate Limiting** - Sliding window algorithm

See [authentication.md](authentication.md) for implementation details.
