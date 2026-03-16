# emc.camus.api

REST API host layer for the Camus application, wiring controllers, middleware, and adapter registrations into the
ASP.NET Core pipeline.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture](../../../docs/architecture.md) |
[Authentication](../../../docs/authentication.md)

---

## 📋 Overview

This project is the composition root of the Camus system. It owns the HTTP pipeline, controller routing,
request/response models, and dependency-injection wiring. Domain and Application layers are referenced as project
dependencies; every infrastructure capability is provided by a swappable adapter registered through extension methods
in `Extensions/`.

---

## ✨ Features

- 🌐 **API Versioning** — URL-segment and header-based versioning (v1, v2)
- 🔐 **Authentication & Authorization** — JWT Bearer and API Key schemes with permission-based policies
- 🛡️ **Global Error Handling** — Centralized exception-to-ProblemDetails middleware with configurable error-code mapping
- 📊 **Observability** — OpenTelemetry tracing, metrics, and structured logging wired at startup
- ⚡ **Rate Limiting** — IP-based sliding-window policies (default, strict, relaxed)
- 📝 **Swagger / OpenAPI** — Auto-generated documentation with typed examples and security schemes
- 🔄 **CORS** — Configurable cross-origin policy
- 🗄️ **Persistence Selection** — InMemory or PostgreSQL chosen per feature via configuration
- 🔑 **Secret Management** — Dapr-based secret provider loaded at startup

---

## 🗂️ Project Structure

| Folder / File | Purpose |
| --- | --- |
| `Controllers/` | Versioned API controllers inheriting `ApiControllerBase` |
| `Configurations/` | Strongly-typed settings classes (`CorsSettings`, `ErrorHandlingSettings`) |
| `Extensions/` | One `*SetupExtensions.cs` file per cross-cutting concern for DI registration |
| `Infrastructure/` | Framework-dependent service implementations (e.g., `HttpUserContext`) |
| `Mapping/` | Request → Command and Result → Response mappers, versioned per API version |
| `Metrics/` | Custom OpenTelemetry meter and counter definitions (`ErrorMetrics`) |
| `Middleware/` | Pipeline middleware (`ExceptionHandlingMiddleware`, `UsernameHeaderMiddleware`) |
| `Models/Dtos/` | Data-transfer objects returned inside response envelopes |
| `Models/Requests/` | Input models bound from `[FromBody]` or `[FromQuery]` |
| `Models/Responses/` | Response envelopes (`ApiResponse<T>`, `PagedResponse<T>`) |
| `SwaggerExamples/` | `IExamplesProvider<T>` classes per API version |
| `Program.cs` | Composition root — ordered adapter registration and middleware pipeline |

---

## 🚀 Usage

### Running Locally

Start the API with the VS Code **run-api** task or from a terminal:

```shell
dotnet run --project src/Api/emc.camus.api/emc.camus.api.csproj
```

Hot-reload is available through the **watch-api** task.

> **📖 Development Setup:** See [Debugging Guide](../../../docs/debugging.md) for Docker Compose and VS Code
debugger attachment.

### Pipeline Order

`Program.cs` registers services and middleware in a numbered sequence. The order is intentional — for example,
rate limiting runs before authentication to block brute-force attacks, and the error-handling middleware sits early
in the pipeline to capture exceptions from all downstream components. Refer to the inline step comments in
`Program.cs` for the rationale behind each position.

---

## ⚙️ Configuration

### CorsSettings

```json
{
  "CorsSettings": {
    "PolicyName": "ClientCors",
    "AllowedOrigins": ["https://app.camus.com/"],
    "AllowedMethods": ["GET", "POST"],
    "AllowedHeaders": ["Content-Type", "Authorization", "Api-Key"],
    "ExposedHeaders": ["Trace-Id", "Username", "Retry-After"],
    "AllowCredentials": true,
    "PreflightMaxAgeMinutes": 5
  }
}
```

### ErrorHandlingSettings

Additional error-code mapping rules evaluated before the platform defaults:

```json
{
  "ErrorHandlingSettings": {
    "AdditionalRules": [
      {
        "Type": "CustomException",
        "Pattern": "custom.*pattern",
        "ErrorCode": "custom_error"
      }
    ]
  }
}
```

> **📖 Other Sections:** JWT, API Key, Rate Limiting, Swagger, Observability, Persistence, and Secret settings
are owned by their respective adapter READMEs. See [Documentation Hub](../../../docs/README.md) for links.

---

## 🔗 Integration

### Adapter Registration

Each adapter exposes a pair of extension methods consumed in `Program.cs`:

| Adapter | Builder method | App method |
| --- | --- | --- |
| Observability | `AddObservability()` | `UseObservability()` |
| Error Handling | `AddErrorHandling()` | `UseErrorHandling()` |
| API Versioning | `AddApiVersioning()` | — |
| Swagger | `AddSwaggerDocumentation()` | `UseSwaggerDocumentation()` |
| CORS | `AddCorsPolicy()` | `UseCorsPolicy()` |
| Rate Limiting | `AddInMemoryRateLimiting()` | `UseInMemoryRateLimiting()` |
| Dapr Secrets | `AddDaprSecrets()` | `UseDaprSecrets()` |
| DB Migrations | `AddDatabaseMigrations()` | `UseDatabaseMigrations()` |
| Cache | `AddInMemoryCache()` | — |
| JWT Auth | `AddJwtAuthentication()` | — |
| API Key Auth | `AddApiKeyAuthentication()` | — |
| Authorization | `AddAuthorizationWithData()` | `UseAuthorizationWithData()` |
| App Services | `AddApplicationServices()` | `UseApplicationServices()` |
| App Data | `AddAppData()` | `UseAppData()` |

### Controllers

| Controller | Versions | Auth | Description |
| --- | --- | --- | --- |
| `ApiInfoController` | v1, v2 | Anonymous / API Key / JWT | Public and protected API information endpoints |
| `AuthController` | v2 | API Key, JWT | User authentication, token generation, listing, and revocation |

### Response Envelope

All success responses are wrapped in `ApiResponse<T>` containing `Message`, `Data`, and `Timestamp` properties.
Error responses use RFC 7807 `ProblemDetails`, generated automatically by `ExceptionHandlingMiddleware`.

### Metrics

The API layer exports:

#### `error_responses_total`

**Type:** Counter  
**Unit:** responses  
**Description:** Total error responses returned by the application

**Dimensions:**

- `error_code` — Machine-readable error code (e.g., `jwt_token_expired`, `rate_limit_exceeded`)
- `http_status` — HTTP status code (401, 429, 500, etc.)
- `path` — Endpoint path that produced the error

---

## 🧪 Troubleshooting

| Symptom | Likely Cause |
| --- | --- |
| `403 Forbidden` on an endpoint that should be public | Missing `[AllowAnonymous]` attribute or CORS policy blocking the origin |
| `429 Too Many Requests` in development | Rate limit policy too strict — check `RateLimitSettings.Policies` in `appsettings.Development.json` |
| Swagger UI not loading | `SwaggerSettings.Enabled` is `false` in the active configuration profile |
| `500` with "secret" or "configuration" in logs | Dapr sidecar not running or secret store misconfigured — see [Dapr Secrets Adapter](../../Adapters/emc.camus.secrets.dapr/README.md) |
| CORS preflight failures | `AllowedOrigins` does not include the requesting origin — update `CorsSettings` |
| Missing `Username` / `Trace-Id` headers | Middleware pipeline order incorrect or observability adapter not registered |
