# Camus API Template

A modern, production-ready **.NET 9.0 REST API template** built with **Hexagonal Architecture (Ports & Adapters)**. This
template provides infrastructure adapters and architectural patterns for building scalable, maintainable, and secure
REST APIs.

> **📘 New to this project?** Start with the [Documentation Index](docs/README.md) for comprehensive guides on
architecture, authentication, deployment, and debugging.

---

## 🎯 What This Template Provides

**Ready-to-use Infrastructure Adapters:**

- 🔐 Authentication (JWT Bearer + API Key)
- �️ Rate Limiting (Sliding Window Algorithm)
- ⏱️ Request Timeouts (Per-Endpoint Policies)
- 📊 Observability (OpenTelemetry + Serilog)
- 🗄️ Data Persistence (PostgreSQL + Dapper)
- 🔒 Secrets Management (Dapr)
- 📚 API Documentation (Swagger/OpenAPI)

**Architectural Foundation:**

- Clean separation between domain, application, and infrastructure layers
- Dependency inversion with port/adapter pattern
- Comprehensive test project structure
- Docker containerization with hot-reload support

---

## 📂 Project Structure

```text
src/
├── Api/                                    # 🌐 REST API Layer
│   └── emc.camus.api/
│       ├── Configurations/                 # App settings bindings
│       ├── Controllers/                    # API endpoints
│       ├── Extensions/                     # Service configuration
│       ├── Infrastructure/                 # Cross-cutting infra
│       ├── Mapping/                        # DTO mapping
│       ├── Metrics/                        # Custom metrics
│       ├── Middleware/                     # HTTP pipeline components
│       ├── Models/                         # Request/response models
│       ├── SwaggerExamples/                # OpenAPI examples
│       └── Program.cs                      # Application startup
│
├── Application/                            # 🔧 Use Cases & Ports
│   └── emc.camus.application/
│       ├── ApiInfo/                       # API info contracts
│       ├── Auth/                          # Authentication interfaces
│       ├── Common/                        # Shared types
│       ├── Configurations/                # Settings contracts
│       ├── Exceptions/                    # Application exceptions
│       ├── Idempotency/                   # Idempotency contracts
│       ├── Observability/                 # Tracing interfaces
│       ├── RateLimiting/                  # Rate limiting contracts
│       └── Secrets/                       # Secret provider interfaces
│
├── Domain/                                 # 💼 Business Core
│   └── emc.camus.domain/
│       ├── Auth/                          # Authentication models
│       └── Exceptions/                    # Domain exceptions
│
├── Adapters/                              # 🔌 Infrastructure
│   ├── emc.camus.cache.inmemory/          # Token revocation cache
│   ├── emc.camus.documentation.swagger/   # Swagger/OpenAPI
│   ├── emc.camus.migrations.dbup/         # Database migrations
│   ├── emc.camus.observability.otel/      # OpenTelemetry
│   ├── emc.camus.persistence.inmemory/    # In-memory repositories
│   ├── emc.camus.persistence.postgresql/  # Database adapter
│   ├── emc.camus.ratelimiting.inmemory/   # Rate limiting
│   ├── emc.camus.secrets.dapr/            # Dapr secrets
│   ├── emc.camus.security.apikey/         # API Key authentication
│   └── emc.camus.security.jwt/            # JWT authentication
│
├── Infrastructure/                        # 🏗️ Infrastructure Config
│   ├── dapr/                             # Dapr configurations
│   └── observability/                    # Observability stack configs
│
└── Test/                                  # 🧪 Testing Projects
    ├── emc.camus.api.integration.test/    # Integration tests (Testcontainers)
    ├── emc.camus.api.test/
    ├── emc.camus.application.test/
    ├── emc.camus.cache.inmemory.test/
    ├── emc.camus.documentation.swagger.test/
    ├── emc.camus.domain.test/
    ├── emc.camus.migrations.dbup.test/
    ├── emc.camus.observability.otel.test/
    ├── emc.camus.persistence.inmemory.test/
    ├── emc.camus.persistence.postgresql.test/
    ├── emc.camus.ratelimiting.inmemory.test/
    ├── emc.camus.secrets.dapr.test/
    ├── emc.camus.security.apikey.test/
    └── emc.camus.security.jwt.test/
```

> **📖 Learn More:** See [Architecture Guide](docs/architecture.md) for detailed layer responsibilities
and dependency flow.

---

## 🚀 Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional, for containerization)
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/) (optional, for local Dapr development)

### Run Locally

1. **Clone and navigate**:

   ```bash
   git clone <your-repo>
   cd camus-template
   ```

2. **Configure secrets** (Development):
  
   Edit `src/Infrastructure/dapr/secrets.json` with your development credentials. See the
   [Dapr Components README](src/Infrastructure/dapr/README.md) for the secrets file format and examples.

3. **Run the API**:

   ```bash
   dotnet run --project src/Api/emc.camus.api/emc.camus.api.csproj
   ```

4. **Explore**:
   - Swagger UI: <http://localhost:5000/swagger>
   - Authenticate: `POST /api/v2/auth/authenticate`

### Run with Docker

```bash
# Development (with hot-reload)
docker-compose -f docker-compose.dev.yml up --build

# Production
docker-compose -f docker-compose.prod.yml up -d
```

> **📖 Detailed Guide:** See [Debugging Documentation](docs/debugging.md) for Docker development workflow
with VS Code debugging.

---

## 🏗️ Architecture Overview

Camus follows **Hexagonal Architecture** (Ports & Adapters):

```text
┌─────────────────────────────────────────────┐
│           Adapters (External)               │
│  API | PostgreSQL | Dapr | OpenTelemetry   │
├─────────────────────────────────────────────┤
│         Application Layer                   │
│  Use Cases | Port Interfaces                │
├─────────────────────────────────────────────┤
│         Domain Layer (Core)                 │
│  Business Logic | Domain Models             │
└─────────────────────────────────────────────┘
```

**Key Benefits:**

- ✅ Domain logic isolated from infrastructure
- ✅ Easy adapter swapping (PostgreSQL → MongoDB)
- ✅ Testable without external dependencies
- ✅ Clear separation of concerns

> **📖 Deep Dive:** Read [Architecture Documentation](docs/architecture.md) for layer responsibilities, dependency
flow, and patterns.

---

## 🧪 Testing

Comprehensive test suite with unit and integration tests — see [Test README](src/Test/README.md) for details
on project structure, running tests, coverage reports, and conventions.

---

## 🚢 Deployment

> **📖 Complete Guide:** See [Deployment Documentation](docs/deployment.md) for Docker builds, production setup,
scaling, and cloud deployment.

---

## 🛠️ Extending the Template

This project uses an **agent-driven SDLC workflow** to implement new features end-to-end — from user stories through
architecture, TDD, implementation, and review. See the [Agentic SDLC Workflow](docs/agentic-sdlc-workflow.md) guide
for the full pipeline, agent roles, and approval gates.

---

## 📚 Documentation

**Getting Started:**

- [📖 Documentation Index](docs/README.md) - Complete documentation guide
- [🏗️ Architecture](docs/architecture.md) - System design and patterns
- [🔐 Authentication](docs/authentication.md) - JWT & API Key implementation
- [🐛 Debugging](docs/debugging.md) - Development workflow
- [🚢 Deployment](docs/deployment.md) - Production deployment

**Adapter Documentation:**

- [Observability (OpenTelemetry)](src/Adapters/emc.camus.observability.otel/README.md)
- [Rate Limiting (Memory)](src/Adapters/emc.camus.ratelimiting.inmemory/README.md)
- [Security (JWT)](src/Adapters/emc.camus.security.jwt/README.md)
- [Security (API Key)](src/Adapters/emc.camus.security.apikey/README.md)
- [Secrets (Dapr)](src/Adapters/emc.camus.secrets.dapr/README.md)
- [Persistence (PostgreSQL)](src/Adapters/emc.camus.persistence.postgresql/README.md)
- [Migrations (DbUp)](src/Adapters/emc.camus.migrations.dbup/README.md)
- [Cache (Memory)](src/Adapters/emc.camus.cache.inmemory/README.md)
- [Persistence (Memory)](src/Adapters/emc.camus.persistence.inmemory/README.md)
- [Documentation (Swagger)](src/Adapters/emc.camus.documentation.swagger/README.md)

---

## 💡 When to Use This Template

**✅ Ideal For:**

- Microservices with clean architecture
- APIs requiring strong observability
- Projects with multiple integrations
- Teams valuing maintainability and testability

**⚠️ Consider Alternatives:**

- Simple CRUD APIs (might be overkill)
- Rapid prototyping (more structure than needed)

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for branching conventions, versioning standard, changelog format, PR
requirements, and the agent-driven development workflow.

---

## 🔒 Security

**Reporting Vulnerabilities:** See [SECURITY.md](SECURITY.md)

**Best Practices:**

- Never commit secrets
- Use environment variables in production
- Rotate credentials regularly
- Enable HTTPS in production

---

Built with ❤️ using .NET 9.0 and Hexagonal Architecture principles.
