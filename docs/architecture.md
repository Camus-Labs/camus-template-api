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

**Role:** Shared contracts layer defining interfaces, attributes, exceptions, and constants.

**Contains:**

- Port interfaces consumed by API layer or multiple adapters
- Attributes for declarative behavior (`[RateLimit]`)
- Custom exceptions for standardized error handling
- Application-wide constants (error codes, headers, policies, meter names)

**Does NOT contain:**

- Implementations (belong in Adapters)
- Use cases or orchestration (belong in API controllers)
- Business logic (belongs in Domain)
- Infrastructure concerns (databases, HTTP, logging)

**Key Namespaces:**

- `Auth/` - Authentication interfaces (`IJwtTokenGenerator`)
- `Observability/` - Telemetry interfaces (`IActivitySourceWrapper`)
- `Secrets/` - Secret management interfaces (`ISecretProvider`)
- `RateLimiting/` - Rate limit attribute and policy constants
- `Generic/` - Application-wide constants (ErrorCodes, Headers, MediaTypes)
- `Exceptions/` - Custom exceptions (RateLimitExceededException)

> **📖 Full Reference:** See [Application Layer README](../src/Application/emc.camus.application/README.md) for complete contracts documentation.

### Adapters

- **API** (`emc.camus.api`) - REST endpoints, middleware
- **Persistence** (`emc.camus.persistence.postgresql`) - Database access with Dapper
- **Observability** (`emc.camus.observability.otel`) - OpenTelemetry integration
- **Rate Limiting** (`emc.camus.ratelimiting.inmemory`) - IP-based sliding window rate limiter
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
- **API Key Authentication** - Header-based (`Api-Key`)
- **CORS** - Configurable policies
- **Rate Limiting** - Sliding window algorithm (memory-based, Redis-ready)

See [authentication.md](authentication.md) for implementation details.
