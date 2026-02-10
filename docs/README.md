# Camus Documentation Hub

Complete technical documentation for the Camus API Template.

---

## 📚 Core Documentation

### [Architecture](architecture.md)

Learn about the system design, layer responsibilities, dependency flow, and architectural patterns. Understand how Domain, Application, and Adapter layers interact.

**Topics covered:**

- Layer structure and responsibilities
- Dependency inversion principle
- Adapter pattern implementation
- Observability stack architecture
- Security architecture

---

### [Authentication](authentication.md)

Complete guide to the authentication system including JWT token generation, API Key authentication, and security configuration.

**Topics covered:**

- JWT Bearer authentication with RSA256
- API Key authentication (`X-Api-Key` header)
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

## 🔌 Adapter Documentation

Detailed usage guides for infrastructure adapters:

- **[Observability (OpenTelemetry)](../src/Adapters/emc.camus.observability.otel/README.md)** - Tracing, metrics, and logging configuration
- **[Security (JWT)](../src/Adapters/emc.camus.security.jwt/README.md)** - JWT authentication setup and configuration
- **[Secrets (Dapr)](../src/Adapters/emc.camus.secrets.dapr/README.md)** - Dapr secret provider usage
- **[Persistence (PostgreSQL)](../src/Adapters/emc.camus.persistence.postgresql/README.md)** - Database adapter and repository pattern
- **[Documentation (Swagger)](../src/Adapters/emc.camus.documentation.swagger/README.md)** - OpenAPI/Swagger configuration

---

## ⚙️ Component Configuration

Infrastructure component setup guides:

- **[Observability Stack](../src/Infrastructure/observability/README.md)** - Jaeger, Prometheus, Grafana, Loki setup
- **[Dapr Components](../src/Infrastructure/dapr/README.md)** - Dapr secret store and component configuration

---

## 🧪 Testing & Development

- **[Postman Collection](../src/Postman/)** - API request examples and test scenarios
- **Test Projects** - See `src/Test/` for comprehensive test project structure

---

## 🔗 Quick Links

- [← Main README](../README.md) - Project overview and quick start
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

## 📝 Documentation Principles

This documentation follows these principles:

- **No Duplication**: Each document covers a specific topic without repeating content
- **Cross-Referenced**: Documents link to related content instead of duplicating it
- **Layered**: Start with overviews, dive deeper in specialized documents
- **Up-to-Date**: Documentation lives close to code (adapter READMEs in adapter folders)

---

Need help? Create an issue or refer to the specific documentation section above.
