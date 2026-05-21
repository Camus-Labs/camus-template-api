# Documentation Hub

Complete technical documentation for the Camus API.

---

## 📚 Core Documentation

### [Architecture](architecture.md)

Learn about the system design, layer responsibilities, dependency flow, and architectural patterns. Understand how
Domain, Application, and Adapter layers interact.

**Topics covered:**

- Layer structure and responsibilities
- Dependency inversion principle
- Adapter pattern implementation
- Cross-cutting concerns (observability, security, caching, idempotency, rate limiting, migrations)

---

### [Authentication](authentication.md)

Complete guide to the authentication system including JWT token generation, API Key authentication, and
security configuration.

**Topics covered:**

- JWT Bearer authentication with RSA256
- API Key authentication (`Api-Key` header)
- Token endpoint usage and claims
- Secret management integration
- Security best practices

---

### [Debugging](debugging.md)

Development setup with hot-reload, VS Code debugging support, and container-based development workflow.

**Topics covered:**

- Docker Compose development setup
- VS Code debugger attachment to containers
- Hot-reload configuration
- Development vs production containers
- VS Code tasks for common operations

---

### [Deployment](deployment.md)

Production deployment strategies including Docker, Azure Container Apps, scaling configuration, and health checks.

**Topics covered:**

- Docker production builds
- Azure Container Apps deployment
- Environment configuration
- Scaling and performance
- Health check endpoints

---

### [Agentic SDLC Workflow](agentic-sdlc-workflow.md)

Agent-driven software development lifecycle workflow and conventions.

**Topics covered:**

- Agent phases and approval gates
- Story-driven development workflow
- Quality checks and review process

---

## 🔌 Adapter Documentation

Detailed usage guides for infrastructure adapters:

- **[Observability (OpenTelemetry)](../src/Adapters/emc.camus.observability.otel/README.md)** - Tracing, metrics, and
  logging configuration
- **[Rate Limiting (Memory)](../src/Adapters/emc.camus.ratelimiting.inmemory/README.md)** - IP-based sliding window rate
  limiting
- **[Security (JWT)](../src/Adapters/emc.camus.security.jwt/README.md)** - JWT authentication setup and configuration
- **[Security (API Key)](../src/Adapters/emc.camus.security.apikey/README.md)** - API Key authentication
  setup and configuration
- **[Secrets (Dapr)](../src/Adapters/emc.camus.secrets.dapr/README.md)** - Dapr secret provider usage
- **[Persistence (PostgreSQL)](../src/Adapters/emc.camus.persistence.postgresql/README.md)** - Database adapter and
  repository pattern
- **[Migrations (DbUp)](../src/Adapters/emc.camus.migrations.dbup/README.md)** - Database schema versioning with DbUp
- **[Cache (Memory)](../src/Adapters/emc.camus.cache.inmemory/README.md)** - Token revocation and idempotency
  response caching
- **[Persistence (Memory)](../src/Adapters/emc.camus.persistence.inmemory/README.md)** - In-memory repositories for
  development and testing
- **[Documentation (Swagger)](../src/Adapters/emc.camus.documentation.swagger/README.md)** -
  OpenAPI/Swagger configuration

---

## Story Template

- **[User Story Template](stories/_user_story_template.md)** - Standard template for writing user stories

### Completed Stories

- **[US-01: Sort Generated Tokens](stories/done/tokens-sorting/US-01-sort-generated-tokens.md)** -
  Token listing sort support
- **[US-01: Idempotency Key Enforcement](stories/todo/idempotency-post-endpoints/US-01-idempotency-key-enforcement.md)**
  \- Require idempotency keys on POST endpoints
- **[US-02: Idempotent Response Caching](stories/todo/idempotency-post-endpoints/US-02-idempotent-response-caching.md)**
  \- Cache and replay responses for duplicate requests
- **[US-03: Apply Idempotency to POST Endpoints](stories/todo/idempotency-post-endpoints/US-03-apply-idempotency-to-post-endpoints.md)**
  \- Apply idempotency key enforcement to existing POST endpoints

---

## 📦 Layer Documentation

Core layer architecture and contracts:

- **[Domain Layer](../src/Domain/emc.camus.domain/README.md)** - Business entities, invariants, and exception types
- **[API Layer](../src/Api/emc.camus.api/README.md)** - Controllers, middleware, pipeline wiring, and response models
- **[Application Layer](../src/Application/emc.camus.application/README.md)** - Shared contracts, interfaces,
  attributes, and constants

---

## ⚙️ Component Configuration

Infrastructure component setup guides:

- **[Observability Stack](../src/Infrastructure/observability/README.md)** - Jaeger, Prometheus, Grafana, Loki setup
- **[Dapr Components](../src/Infrastructure/dapr/README.md)** - Dapr secret store and component configuration
- **[Database Migrations](../src/Infrastructure/database/README.md)** - Schema management, migration scripts, and
  versioning

---

## 🧪 Testing & Development

- **[Postman Collection](postman/)** - API request examples and test scenarios
- **[Test Projects](../src/Test/README.md)** - Project mapping, coverage reports, and testing conventions

---

## 🔗 Quick Links

- [← Main README](../README.md) - Project overview and quick start
- [Changelog](../CHANGELOG.md) - Version history and release notes
- [Security Policy](../SECURITY.md) - Vulnerability reporting
- [API Reference](http://localhost:5000/swagger) - Interactive Swagger UI (when running)

---

## 🗺️ Documentation Map

**Getting Started:**

1. Start with [Main README](../README.md) for project overview
2. Review [Architecture](architecture.md) to understand system design
3. Configure [Authentication](authentication.md) for API access
4. Set up [Debugging](debugging.md) environment for development

**Going to Production:**

1. Review adapter READMEs for configuration options
2. Configure observability stack for monitoring
3. Follow [Deployment](deployment.md) guide for production setup
4. Review [Security Policy](../SECURITY.md) for security best practices

---

Need help? Create an issue or refer to the specific documentation section above.
