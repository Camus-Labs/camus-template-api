
# Welcome to CamusTemplate API

A modern, production-ready **.NET 9.0 REST API template** designed with **Hexagonal Architecture (Ports & Adapters)**.  
Provides a robust foundation for scalable, maintainable, and observable APIs, with a strong focus on **security**, **clean code**, and **cloud-native best practices**.

---

## 🚀 Key Features

- **Hexagonal Architecture (Ports & Adapters)**  
  Decouples business logic from infrastructure, enabling easy testing and adaptability to new technologies.

- **.NET 9.0**  
  Leverages the latest .NET features for performance and maintainability.

- **Security**  
  - JWT Bearer Authentication (OAuth2, RSA256)
  - API Key Authentication (`X-Api-Key` header)
  - CORS and Rate Limiting policies
  - Strict Security Headers (CSP, HSTS, etc.)

- **OpenAPI/Swagger**  
  - API documentation with versioning (v1, v2)
  - JWT & API Key support in Swagger UI (Authorize button)
  - Example requests/responses

- **Observability**  
  - OpenTelemetry for distributed tracing & metrics
  - Console, Jaeger, Zipkin, Azure Monitor, Prometheus exporters
  - Serilog logging with Elasticsearch integration

- **Repository Pattern**  
  - Clean data access separation
  - PostgreSQL adapter with Dapper ORM
  - Generic repository interfaces

- **Cloud-Native Ready**  
  - Azure Container Apps optimized
  - Optional Dapr integration for microservices
  - Docker support

- **Enterprise Features**  
  - Global exception handling
  - Structured logging
  - API versioning (URL & Header)
  - Rate limiting & CORS

---

## 🏗️ Project Structure

<details>
<summary>Click to expand</summary>

```
src/
├── projectName.sln
├── Dockerfile
├── Api/
│   └── emc.main.api/                # API layer (Controllers, Program.cs)
│       ├── Controllers/
│       │   └── AuthController.cs
│       ├── Handlers/
│       │   ├── ApiKeyAuthenticationHandler.cs
│       │   └── ExceptionHandlingMiddleware.cs
│       └── Program.cs
├── Application/
│   └── emc.application/              # Application services/use cases
├── Domain/
│   └── emc.domain/                   # Domain models/business logic
│       ├── Auth/
│       ├── Generic/
│       ├── Logging/s
│       └── SwaggerExamples/
├── Adapters/
│   └── emc.datapersistance.postgresql/
│       ├── Repository/
│       │   └── PostgreSqlRepo.cs
│       ├── DTOs/
│       └── Mappers/
└── Test/
    ├── Api.Test/
    ├── Application.Test/
    ├── Domain.Test/
    └── Adapter.postgresql.Test/
```

</details>

---

## 🔐 Security

- **JWT Authentication**
  - RSA256 (certificate-based signing)
  - Configurable issuer, audience, RSA keys (`appsettings.json`)
  - Token endpoint:  
    ```
    POST /api/v{version}/auth/token
    ```

- **API Key Authentication**
  - Header: `X-Api-Key`
  - Configurable in application settings
  - Custom authentication handler

- **Swagger UI Security**
  - "Authorize" button (JWT & API Key)
  - Bearer format support
  - Security requirement per endpoint

- **Additional**
  - CORS policies (configurable)
  - Rate limiting (sliding window)
  - HSTS (production)
  - Global sanitized error handling

---

## 📈 Observability

- **OpenTelemetry**
  - Distributed tracing (custom activity sources)
  - Metrics with Prometheus endpoint (`/metrics`)
  - ASP.NET Core, HTTP client, runtime instrumentation

- **Exporters**
  - Console (development)
  - Jaeger (default port 6831)
  - Zipkin
  - Azure Monitor
  - Prometheus

- **Logging**
  - Serilog (structured logs)
  - Elasticsearch
  - Console
  - OpenTelemetry exporters

- **Configuration**
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

---

## 📚 API Documentation

### Swagger / OpenAPI
- URL: `/swagger` (redirect from `/`)
- Multi-version: v1.0, v2.0
- Interactive testing with authentication
- XML docs integrated
- Example request/response models

### Authentication Endpoints

- **Generate JWT:**  
  ```
  POST /api/v{version}/auth/token
  ```

- **API Info:**  
  ```
  GET /api/v{version}/auth/info
  ```

### API Versioning

- URL segment: `/api/v1/`, `/api/v2/`
- Header-based:  
  ```
  X-Api-Version: 1.0
  ```

---

## 🗄️ Database Support

### PostgreSQL Integration

- **ORM:** Dapper for high-performance access
- **Pattern:** Generic repository with `IPostgreSqlRepo<T>`
- **Features:** Stored procedures & scalar functions

#### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=dbinventorycentraletl;Username=user;Password=password"
  }
}
```

---

## 🏃‍♂️ Running Locally

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL (optional, for DB features)

### Build & Run

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/Api/emc.main.api/emc.camus.main.api.csproj

# Run tests
dotnet test

# Check for outdated packages
dotnet list package --outdated
```

### Access Points

- API: [http://localhost:5000](http://localhost:5000)
- Swagger UI: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- Metrics: [http://localhost:5000/metrics](http://localhost:5000/metrics) *(if Prometheus enabled)*

---

## ⚙️ Configuration

### JWT Settings

```json
{
  "JwtSettings": {
    "RsaPrivateKeyPem": "certificate.pem",
    "Issuer": "https://dbinventorycentraletl.azapipg.com",
    "Audience": "https://dbinventorycentraletl.azapipg.com"
  }
}
```

### API Key

```json
{
  "ApiKey": "your-secure-api-key"
}
```

---

## ☁️ Deployment

### Azure Container Apps

- **Container-ready:** Dockerfile included
- **Config:** Use environment variables for production
- **Scaling:** Stateless, supports horizontal scale

### Dapr Integration

- **Service Invocation:** Microservices-ready
- **State Management:** Pluggable state stores
- **Pub/Sub:** Event-driven architecture support

### Docker Commands

```bash
# Build container
docker build -t dbinventorycentraletl-api .

# Run container
docker run -p 8080:8080 dbinventorycentraletl-api
```

---

## 👩‍💻 Development Guidelines

### Testing

- **Unit Tests:** Domain & application logic
- **Integration Tests:** API endpoints, database
- **Mocking:** Moq for dependencies
- **Coverage:** Coverlet for analysis

### Code Quality

- Nullable Reference Types: Enabled
- .NET Analyzers: Built-in
- XML Documentation: For APIs
- SOLID Principles & Clean Architecture

---

## 🛠️ Extending the Template

### Add New Adapters

1. Create new project in `Adapters/`
2. Implement port interfaces from `Application/`
3. Register services in `Program.cs`

### Add New Use Cases

1. Define interfaces in `Application/`
2. Implement logic following domain patterns
3. Create controllers in `Api/`

### Add New Domain Models

1. Create models in `Domain/`
2. Define repository interfaces
3. Implement adapters as needed

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch  
   ```
   git checkout -b feature/amazing-feature
   ```
3. Write tests for your changes
4. Ensure all tests pass  
   ```
   dotnet test
   ```
5. Update documentation as needed
6. Submit a Pull Request

### Code Standards

- Follow naming conventions
- Add XML docs for public APIs
- Include unit tests
- Update README for new features

---

## 🙋 Support

- Check the documentation
- Review existing issues
- Create new issues for bugs/requests

---

## ❤️ Credits

Built with:

- **.NET 9.0** – Microsoft's latest framework  
- **OpenTelemetry** – Cloud-native observability  
- **Swagger/OpenAPI** – API documentation  
- **PostgreSQL** – Reliable database engine  
- **Docker** – Containerization platform  

---