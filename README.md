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
│       ├── Controllers/                    # API endpoints
│       ├── Middleware/                     # HTTP pipeline components
│       ├── Extensions/                     # Service configuration
│       └── Program.cs                      # Application startup
│
├── Application/                            # 🔧 Use Cases & Ports
│   └── emc.camus.application/
│       ├── Auth/                          # Authentication interfaces
│       ├── Observability/                 # Tracing interfaces
│       └── Secrets/                       # Secret provider interfaces
│
├── Domain/                                 # 💼 Business Core
│   └── emc.camus.domain/
│       ├── Auth/                          # Authentication models
│       └── Generic/                       # Base entities
│
├── Adapters/                              # 🔌 Infrastructure
│   ├── emc.camus.persistence.postgresql/  # Database adapter
│   ├── emc.camus.secrets.dapr/           # Dapr secrets
│   ├── emc.camus.observability.otel/     # OpenTelemetry
│   ├── emc.camus.ratelimiting.inmemory/    # Rate limiting
│   ├── emc.camus.security.jwt/           # JWT authentication
│   ├── emc.camus.security.apikey/        # API Key authentication
│   └── emc.camus.documentation.swagger/  # Swagger/OpenAPI
│
├── Infrastructure/                        # 🏗️ Infrastructure Config
│   ├── dapr/                             # Dapr configurations
│   └── observability/                    # Observability stack configs
│
└── Test/                                  # 🧪 Testing Projects
    ├── emc.camus.api.test/
    ├── emc.camus.application.test/
    ├── emc.camus.domain.test/
    ├── emc.camus.persistence.postgresql.test/
    ├── emc.camus.observability.otel.test/
    ├── emc.camus.secrets.dapr.test/
    ├── emc.camus.security.apikey.test/
    └── emc.camus.security.jwt.test/
```

> **📖 Learn More:** See [Architecture Guide](docs/architecture.md) for detailed layer responsibilities and dependency flow.

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
  
   Edit `src/Infrastructure/dapr/secrets.json`:

   ```json
   {
     "AccessKey": "dev-access-key",
     "AccessSecret": "dev-access-secret",
     "XApiKey": "dev-api-key-12345",
     "RsaPrivateKeyPem": "<your-rsa-private-key>"
   }
   ```

3. **Run the API**:

   ```bash
   dotnet run --project src/Api/emc.camus.api/emc.camus.api.csproj
   ```

4. **Explore**:
   - Swagger UI: <http://localhost:5000/swagger>
   - Get JWT Token: `POST /api/v2/auth/token`
   - Health Check: `GET /health`

### Run with Docker

```bash
# Development (with hot-reload)
docker-compose -f docker-compose.dev.yml up --build

# Production
docker-compose -f docker-compose.prod.yml up -d
```

> **📖 Detailed Guide:** See [Debugging Documentation](docs/debugging.md) for Docker development workflow with VS Code debugging.

---

## 🔐 Authentication

The template includes two authentication mechanisms ready to use:

### JWT Bearer Tokens

Send a POST request to `/api/v2/auth/token` with `accessKey` and `accessSecret` credentials to receive a Bearer token. Include the token in the `Authorization: Bearer <token>` header for subsequent requests to protected endpoints.

### API Key

Include your API key in the `X-Api-Key` header on every request to protected endpoints.

> **📖 Full Guide:** See [Authentication Documentation](docs/authentication.md) for configuration, claims, and security
best practices.

---

## �️ Rate Limiting

Built-in IP-based rate limiting with sliding window algorithm:

- **Policy-based configuration** - Multiple limit profiles (strict/default/relaxed)
- **Proxy support** - Automatic X-Forwarded-For header detection
- **RFC-compliant headers** - Standard rate limit response headers
- **OpenTelemetry metrics** - Track rejections and usage patterns

**Apply to endpoints:**

Apply `[RateLimit(RateLimitPolicies.Strict)]` or `[RateLimit(RateLimitPolicies.Relaxed)]` attributes to controllers or actions. See `RateLimitPolicies` in `src/Application/emc.camus.application/RateLimiting/` for available policies.

> **📖 Full Guide:** See [Rate Limiting Adapter README](src/Adapters/emc.camus.ratelimiting.inmemory/README.md) for
configuration and deployment.

---

## 📊 Observability

Built-in OpenTelemetry integration with multiple exporter options:

- **Tracing**: OTLP, Jaeger, Zipkin, Console
- **Metrics**: OTLP, Prometheus, Console
- **Logging**: Serilog with OTLP exporter to Loki

**Quick Setup:**

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Otlp",
      "OtlpEndpoint": "http://localhost:4317"
    }
  }
}
```

The template includes Docker Compose configurations for a complete observability stack (Jaeger, Prometheus, Grafana, Loki).

**📖 Learn More:**

> - [Observability Adapter README](src/Adapters/emc.camus.observability.otel/README.md) - Usage guide
> - [Observability Stack README](src/Infrastructure/observability/README.md) - Stack configuration

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

> **📖 Deep Dive:** Read [Architecture Documentation](docs/architecture.md) for layer responsibilities, dependency flow,
and patterns.

---

## 🧪 Testing

Comprehensive test project structure included:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific project
dotnet test src/Test/emc.camus.api.test/
```

**Test Organization:**

- `emc.camus.api.test` - Controller and middleware tests
- `emc.camus.application.test` - Use case tests
- `emc.camus.domain.test` - Domain logic tests
- `emc.camus.*.test` - Adapter-specific tests

---

## 🚢 Deployment

### Docker

```bash
# Build production image
docker build -t camus-api:latest .

# Run
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production camus-api:latest
```

### Azure Container Apps

```bash
az containerapp create \
  --name camus-api \
  --resource-group camus-rg \
  --environment camus-env \
  --image your-registry/camus-api:latest \
  --target-port 8080 \
  --ingress external
```

> **📖 Complete Guide:** See [Deployment Documentation](docs/deployment.md) for production setup, scaling, and cloud deployment.

---

## 🛠️ Extending the Template

### Add Business Controllers

Create versioned API controllers in `src/Api/emc.camus.api/Controllers/`. Apply `[ApiController]`, `[ApiVersion]`, and version-based route attributes. See existing controllers in that folder for the pattern.

### Implement Use Cases

Add application service interfaces in `src/Application/emc.camus.application/`. See existing interfaces like `IApiInfoRepository` and `IUserRepository` for the contract pattern.

### Create Domain Entities

Define business models in `src/Domain/emc.camus.domain/`. Keep domain entities free of infrastructure dependencies.

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

See [CONTRIBUTING.md](CONTRIBUTING.md) for branching conventions, versioning standard,
changelog format, PR requirements, and the agent-driven development workflow.

---

## 🔒 Security

**Reporting Vulnerabilities:** See [SECURITY.md](SECURITY.md)

**Best Practices:**

- Never commit secrets
- Use environment variables in production
- Rotate credentials regularly
- Enable HTTPS in production

---

## 📄 License

MIT License - see LICENSE file for details.

---

Built with ❤️ using .NET 9.0, OpenTelemetry, and Hexagonal Architecture principles.
