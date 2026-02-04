# Camus API Template

A modern, production-ready **.NET 9.0 REST API template** built with **Hexagonal Architecture (Ports & Adapters)**. This template provides the architectural skeleton and infrastructure setup for building scalable, maintainable, and secure REST APIs with modern best practices baked in from the start.

Think of it as a "starter kit" that handles authentication, observability, security, and infrastructure concerns - giving you a clean architectural foundation to build your business logic upon.

> **Note**: This template focuses on providing the architectural foundation with working infrastructure. Business controllers and domain-specific logic are intentionally left for you to implement using the established patterns.

---

## 🚀 What's Included

### ✅ Infrastructure & Foundation (Ready to Use)

- **Authentication System**
  - JWT Bearer with RSA256 signing
  - API Key authentication (`X-Api-Key`)
  - OAuth2-compatible token endpoint
  
- **API Infrastructure**
  - OpenAPI/Swagger with interactive UI
  - Multi-version support (v1, v2)
  - Global exception handling
  - Rate limiting (100 req/min)
  - CORS policies
  
- **Observability**
  - OpenTelemetry (tracing & metrics)
  - Multiple exporters (Console, Jaeger, Zipkin, Azure Monitor, Prometheus)
  - Serilog structured logging
  - Health check endpoints
  
- **Data Access**
  - PostgreSQL adapter with Dapper
  - Generic repository pattern
  - DTO mapping infrastructure
  
- **Cloud-Native**
  - Docker multi-stage build
  - Azure Container Apps ready
  - Dapr integration (secrets, state, pub/sub)
  - Environment-based configuration

### 🏗️ What You Build (Business Logic)

- **Business Controllers**: Add your domain-specific API endpoints
- **Use Cases**: Implement application services orchestrating business operations
- **Domain Models**: Extend with your business entities and rules
- **Integration Tests**: Build comprehensive test coverage

---

## 📁 Project Structure

```text
src/
├── CamusApp.sln
├── Dockerfile
│
├── Api/                                   # 🌐 REST API Layer
│   └── emc.main.api/
│       ├── Controllers/                   # API endpoints (AuthController provided)
│       ├── Handlers/                      # Middleware & authentication
│       └── Program.cs                     # Application entry point
│
├── Application/                           # 🔧 Use Cases & Ports
│   └── emc.application/
│       └── Data/                          # Port interfaces (contracts)
│
├── Domain/                                # 💼 Business Logic Core
│   └── emc.domain/
│       ├── Auth/                          # Authentication models
│       ├── Entities/                      # Domain models
│       └── Generic/                       # Base classes
│
├── Adapters/                              # 🔌 External Integrations
│   ├── emc.datapersistance.postgresql/   # Database adapter
│   ├── emc.secretstorage.dapr/           # Secret management
│   └── emc.observability.otel/           # OpenTelemetry setup
│
└── Test/                                  # 🧪 Testing Projects
    ├── api.main.test/
    ├── application.test/
    ├── domain.test/
    └── adapter.postgresql.test/
```

---

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker (optional, for containerization)
- PostgreSQL (for database features)

### Run Locally

1. **Clone the repository**:

   ```bash
   git clone <your-repo>
   cd camus-template
   ```

2. **Configure secrets** (Development):
  
   Edit `src/Adapters/emc.adapterdapr.components/secrets.json`:

   ```json
   {
     "AccessKey": "your-access-key",
     "AccessSecret": "your-access-secret",
     "XApiKey": "your-api-key"
   }
   ```

3. **Run the application**:

   ```bash
   dotnet run --project src/Api/emc.main.api/emc.camus.main.api.csproj
   ```

4. **Explore the API**:
   - **Swagger UI**: <http://localhost:5000/swagger>
   - **Get JWT Token**: `POST /api/v1/auth/token` with your credentials
   - **Metrics**: <http://localhost:5000/metrics> (if Prometheus enabled)

### Run with Docker

```bash
docker-compose up --build
```

Access at: <http://localhost:9003/swagger>

---

## 🏗️ Architecture: Hexagonal (Ports & Adapters)

This template implements Hexagonal Architecture, separating your application into distinct layers:

```text
┌─────────────────────────────────────────────────────────┐
│                    Adapters Layer                       │
│  🌐 API Controllers  🗄️ PostgreSQL  🔌 External APIs   │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                 Application Layer                       │
│      🔧 Use Cases  ⚡ Ports (Interfaces)               │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                   Domain Layer (Core)                   │
│      💼 Entities  📋 Business Rules                    │
└─────────────────────────────────────────────────────────┘
```

**Benefits:**

- **Testability**: Easy unit testing without external dependencies
- **Flexibility**: Swap databases/APIs without changing core logic
- **Maintainability**: Clear separation of concerns

**Current Flow (Authentication):**

1. **API Layer**: `AuthController` receives JWT token requests
2. **Application Layer**: Validates credentials via `ISecretProvider` interface
3. **Domain Layer**: Uses authentication models (`JwtTokenRequest`, `JwtTokenResponse`)
4. **Adapter Layer**: `DaprSecretProvider` retrieves credentials from secret store

---

## 🔐 Authentication

### JWT Bearer Tokens

Generate tokens via the authentication endpoint:

```bash
POST /api/v1/auth/token
Content-Type: application/json

{
  "accessKey": "your-access-key",
  "accessSecret": "your-access-secret"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresOn": "2026-02-04T23:55:05.123Z"
}
```

**Use the token:**

```http
Authorization: Bearer {your-token}
```

### API Key Authentication

Alternative authentication via header:

```http
X-Api-Key: your-api-key
```

**Configuration:**

- JWT settings in `appsettings.json` (issuer, audience, expiration)
- Secrets managed via Dapr (production) or `secrets.json` (development)
- RSA256 signing with certificate-based keys

📖 **Detailed Guide**: See [doc/authentication.md](doc/authentication.md)

---

## 📊 Observability

### OpenTelemetry Integration

**Tracing**: Distributed tracing with correlation IDs across services
**Metrics**: ASP.NET Core, HTTP client, and runtime instrumentation
**Exporters**: Console, Jaeger, Zipkin, Azure Monitor, Prometheus

### Configuration

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Console|Jaeger|Zipkin|AzureMonitor"
    },
    "Metrics": {
      "MetricsExporter": "None|Prometheus"
    }
  }
}
```

### Structured Logging

- **Serilog** with Elasticsearch sink support
- Console and file outputs
- Request/response logging with sanitization
- Activity tracing with correlation IDs

---

## 🧪 Testing

### Framework Setup

- **XUnit**: Test framework for all layers
- **Moq**: Mocking dependencies
- **Coverlet**: Code coverage collection

### Test Projects

- `api.main.test/` - API endpoint tests
- `application.test/` - Use case tests
- `domain.test/` - Domain logic tests
- `adapter.postgresql.test/` - Database adapter tests

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test src/Test/api.main.test/
```

---

## ⚙️ Configuration

### Application Settings

Key configuration areas in `appsettings.json`:

- **JWT Settings**: Token issuer, audience, expiration, RSA key path
- **OpenTelemetry**: Exporter selection, endpoints
- **Rate Limiting**: Request limits, time windows
- **CORS**: Allowed origins, methods, headers
- **Database**: Connection strings (use environment variables in production)

### Environment Variables

For production, override settings via environment variables:

```bash
JwtSettings__Issuer=https://api.production.com
ConnectionStrings__DefaultConnection=Host=...
OpenTelemetry__Tracing__Exporter=AzureMonitor
```

### Secrets Management

- **Development**: `src/Adapters/emc.adapterdapr.components/secrets.json`
- **Production**: Azure Key Vault, AWS Secrets Manager, or Dapr secret stores

---

## 🚢 Deployment

### Docker

**Build image:**

```bash
docker build -t camus-api .
```

**Run container:**

```bash
docker run -p 8080:9003 camus-api
```

### Azure Container Apps

The template is optimized for Azure Container Apps:

- Multi-stage Dockerfile for small image size
- Non-root user execution
- Health check endpoints
- Environment-based configuration

### Dapr Integration

Optional Dapr features:

- **Service Invocation**: Microservice communication
- **State Management**: Pluggable state stores
- **Pub/Sub**: Event-driven architecture
- **Secrets**: Centralized secret management

📖 **Deployment Guide**: See [doc/deployment.md](doc/deployment.md)

---

## 🛠️ Extending the Template

### Add a New Business Controller

1. Create controller in `src/Api/emc.main.api/Controllers/`:

   ```csharp
   [ApiController]
   [Route("api/v{version:apiVersion}/[controller]")]
   public class ProductsController : ControllerBase
   {
       // Your endpoints
   }
   ```

2. Register services in `DependencyInjectionHandler.cs`

### Add a New Domain Entity

1. Create model in `src/Domain/emc.domain/Entities/`
2. Define repository interface in `src/Application/emc.application/Data/`
3. Implement adapter in `src/Adapters/emc.datapersistance.postgresql/`

### Add External Integration

1. Create new adapter project in `src/Adapters/`
2. Implement application interfaces (ports)
3. Register in dependency injection

---

## 📚 Documentation

- **[Architecture Guide](doc/architecture.md)** - System design and patterns
- **[Authentication](doc/authentication.md)** - JWT & API Key implementation
- **[Debugging](doc/debugging.md)** - Development workflow with Docker
- **[Deployment](doc/deployment.md)** - Production deployment guide
- **[API Reference](/swagger)** - Interactive Swagger UI

---

## 💡 When to Use This Template

**✅ Perfect For:**

- REST APIs with complex business logic
- Microservices requiring clean architecture
- Applications needing strong observability
- Projects with multiple external integrations
- Teams wanting maintainable, testable code
- Dapr-enabled service mesh architectures

**⚠️ Consider Alternatives If:**

- Building simple CRUD APIs (might be overkill)
- Small teams unfamiliar with hexagonal architecture
- Prototypes requiring rapid iteration

---

## 🔗 Next Steps

1. **Get the Template**: Visit [APIGen Portal](https://apigeninterface-01.ambitiouscoast-ca9f7f6e.eastus2.azurecontainerapps.io/)
2. **Add Business Controllers**: Implement your domain-specific endpoints
3. **Build Use Cases**: Create application services orchestrating business logic
4. **Configure External Systems**: Set up PostgreSQL, monitoring, secrets
5. **Write Tests**: Build comprehensive test coverage
6. **Deploy**: Use Docker/Azure Container Apps for production

**Remember**: This template provides the foundation - build your business logic on top! 🚀

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Write tests for your changes
4. Ensure all tests pass: `dotnet test`
5. Update documentation as needed
6. Submit a Pull Request

### Code Standards

- Follow .NET naming conventions
- Add XML documentation for public APIs
- Include unit tests for new features
- Keep business logic in Domain layer
- Implement ports/adapters for external dependencies

---

## 🔒 Security

**Reporting Vulnerabilities**: See [SECURITY.md](SECURITY.md) for our security policy and how to report vulnerabilities.

**Best Practices**:

- Never commit secrets to version control
- Use environment variables for production
- Rotate credentials regularly
- Keep dependencies updated
- Enable HTTPS in production

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🙋 Support & Questions

- **Documentation**: Check the [doc/](doc/) folder for detailed guides
- **Issues**: Create GitHub issues for bugs or feature requests
- **Questions**: Review existing issues or start a discussion

---

Built with ❤️ using .NET 9.0, OpenTelemetry, and Hexagonal Architecture principles.
