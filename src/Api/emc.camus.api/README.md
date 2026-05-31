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
- 🛡️ **Global Error Handling** — Centralized exception-to-ProblemDetails
  middleware with configurable error-code mapping
- 📊 **Observability** — OpenTelemetry tracing, metrics, and structured logging wired at startup
- ⚡ **Rate Limiting** — IP-based sliding-window policies (default, strict, relaxed)
- 📝 **Swagger / OpenAPI** — Auto-generated documentation with typed examples and security schemes
- 🔄 **CORS** — Configurable cross-origin policy
- 🗄️ **Persistence Selection** — InMemory or PostgreSQL chosen globally via
  `DataPersistenceSettings.Provider` configuration
- 🔑 **Secret Management** — Dapr-based secret provider loaded at startup
- 🔏 **Idempotency** — Header-enforced per-endpoint idempotency key validation and response caching with
  configurable TTL policies and `Idempotency-Key-Status` response header

---

## 🗂️ Project Structure

| Folder / File | Purpose |
| --- | --- |
| `Controllers/` | Versioned API controllers inheriting `ApiControllerBase` |
| `Configurations/` | Strongly-typed settings classes (`ApiKeySettings`, `ApiVersionSettings`, `AuthenticationSchemes`, `CorsSettings`, `ErrorCodeMappingRuleSettings`, `ErrorHandlingSettings`, `IdempotencySettings`, `IdempotencyPolicies`, `JwtSettings`, `MediaTypes`, `RateLimitContextKeys`, `RateLimitPolicies`, `RateLimitPolicySettings`, `RateLimitingSettings`, `RequestTimeoutSettings`, `RequestTimeoutPolicies`, `SwaggerSettings`) |
| `Extensions/` | One `*SetupExtensions.cs` file per cross-cutting concern for DI registration |
| `Utilities/` | Framework-dependent service implementations (`HttpUserContext`, `ClientIpResolver`, `ApiKeyAuthenticationHandler`, `JwtTokenGenerator`) |
| `Mapping/` | Request → Command and Result → Response mappers, versioned per API version |
| `Metrics/` | Custom OpenTelemetry meter and counter definitions (`ErrorMetrics`, `IdempotencyMetrics`, `RateLimitMetrics`) |
| `Middleware/` | Pipeline middleware (`ExceptionHandlingMiddleware`, `RateLimitHeadersMiddleware`, `SecurityHeadersMiddleware`, `UsernameHeaderMiddleware`) |
| `Models/Dtos/` | Data-transfer objects returned inside response envelopes |
| `Models/Requests/` | Input models bound from `[FromBody]` or `[FromQuery]` |
| `Models/Responses/` | Response envelopes (`ApiResponse<T>`, `PagedResponse<T>`) |
| `Filters/` | Action filters and marker attributes (`DefaultApiResponsesOperationFilter`, `IdempotencyKeyValidationFilter`, `IdempotencyResponseCachingFilter`, `RateLimitAttribute`, `RequireIdempotencyKeyAttribute`) |
| `SwaggerExamples/` | `IExamplesProvider<T>` classes per API version |
| `Program.cs` | Composition root — ordered adapter registration and middleware pipeline |

---

## 🚀 Usage

### Running Locally

Start the API with the VS Code **run-api** task. Hot-reload is available through the **watch-api** task.

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
    "ExposedHeaders": ["Content-Type", "Trace-Id", "Retry-After", "RateLimit-Limit", "RateLimit-Reset", "RateLimit-Policy", "RateLimit-Window"],
    "AllowCredentials": true,
    "PreflightMaxAgeMinutes": 5
  }
}
```

### ErrorHandlingSettings

Additional fallback error-code mapping rules appended after the built-in platform defaults:

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

### IdempotencySettings

```json
{
  "IdempotencySettings": {
    "StandardTtlSeconds": 300,
    "LongTermTtlSeconds": 86400
  }
}
```

### RequestTimeoutSettings

```json
{
  "RequestTimeoutSettings": {
    "DefaultTimeoutSeconds": 30,
    "TightTimeoutSeconds": 10,
    "ExtendedTimeoutSeconds": 60
  }
}
```

### RateLimitingSettings

```json
{
  "RateLimitingSettings": {
    "SegmentsPerWindow": 5,
    "DefaultPermitLimit": 250,
    "DefaultWindowSeconds": 60,
    "StrictPermitLimit": 50,
    "StrictWindowSeconds": 60,
    "RelaxedPermitLimit": 500,
    "RelaxedWindowSeconds": 60,
    "ExemptPaths": ["/health", "/ready", "/alive", "/swagger"]
  }
}
```

> **📖 Other Sections:** Observability, Persistence, and Secret settings
are owned by their respective adapter READMEs. See [Documentation Hub](../../../docs/README.md) for links.

---

## 🔗 Integration

### Adapter Registration

Each concern exposes extension methods consumed in `Program.cs` — some expose only a builder
registration method, some only an app middleware method, and others both:

| Adapter | Builder method | App method |
| --- | --- | --- |
| Observability | `AddObservability()` | `UseObservability()` |
| Error Handling | `AddErrorHandling()` | `UseErrorHandling()` |
| Idempotency | `AddIdempotency()` | — |
| API Versioning | `AddApiVersioning()` | — |
| Swagger | `AddSwaggerDocumentation()` | `UseSwaggerDocumentation()` |
| CORS | `AddCorsPolicy()` | `UseCorsPolicy()` |
| Rate Limiting | `AddRateLimiting(serviceName)` | `UseRateLimiting()` |
| Dapr Secrets | `AddDaprSecrets()` | `UseDaprSecrets()` |
| DB Migrations | `AddDatabaseMigrations()` | `UseDatabaseMigrations()` |
| Cache | `AddInMemoryCache()` | — |
| JWT Auth | `AddJwtAuthentication()` | — |
| API Key Auth | `AddApiKeyAuthentication()` | — |
| Security Headers | — | `UseSecurityHeaders()` |
| Transport Security | — | `UseTransportSecurity()` |
| Health Checks | `AddHealthChecks()` | `UseHealthChecks()` |
| Request Timeouts | `AddRequestTimeoutPolicies()` | `UseRequestTimeoutPolicies()` |
| Authorization | `AddAuthorizationPolicies()` | `UseAuthorizationPolicies()` |
| App Services | `AddApplicationServices()` | `UseApplicationServices()` |
| Persistence | `AddPersistence()` | `UsePersistenceAsync()` |

### Controllers

| Controller | Versions | Auth | Description |
| --- | --- | --- | --- |
| `ApiInfoController` | v1, v2 | Anonymous / API Key / JWT | Public and protected API information endpoints |
| `AuthController` | v1, v2 (actions: v2) | API Key, JWT | User authentication, token generation, listing, and revocation |

### Response Envelope

Controller success responses are wrapped in `ApiResponse<T>` containing `Message`, `Data`, and `Timestamp` properties.
Infrastructure endpoints (`/health`, `/alive`, `/ready`) return their own response format.
Error responses use RFC 7807 `ProblemDetails`, generated automatically by `ExceptionHandlingMiddleware`.

### Metrics

The API layer exports:

#### `error_responses_total`

**Type:** Counter  
**Unit:** responses  
**Description:** Total number of error responses returned by the application

- `error_code` — Machine-readable error code (e.g., `jwt_token_expired`, `rate_limit_exceeded`)
- `http_status` — HTTP status code (401, 429, 500, etc.)
- `path` — Endpoint path that produced the error

#### `idempotency_cache_hit_total`

**Type:** Counter
**Unit:** requests
**Description:** Total number of idempotency cache hits (responses replayed from cache)

#### `idempotency_body_conflict_total`

**Type:** Counter
**Unit:** requests
**Description:** Total number of idempotency body conflict rejections (same key, different body)

#### `idempotency_cache_error_total`

**Type:** Counter
**Unit:** errors
**Description:** Total number of idempotency cache infrastructure errors (fail-open)

#### `rate_limit_rejections_total`

**Type:** Counter
**Unit:** requests
**Description:** Total number of requests rejected due to rate limiting

- `policy` — Rate limit policy that was exceeded (`default`, `strict`, `relaxed`)
- `method` — HTTP method of the rejected request

---

## 🧪 Troubleshooting

| Symptom | Likely Cause |
| --- | --- |
| `403 Forbidden` on an endpoint that should be public | Missing `[AllowAnonymous]` attribute or CORS policy blocking the origin |
| `429 Too Many Requests` in development | Rate limit policy too strict — check `RateLimitingSettings` in `appsettings.Development.json` |
| Swagger UI not loading | `SwaggerSettings.Enabled` is `false` or the application is not running in the Development environment |
| `500` with "secret" or "configuration" in logs | Dapr sidecar not running or secret store misconfigured — see [Dapr Secrets Adapter](../../Adapters/emc.camus.secrets.dapr/README.md) |
| CORS preflight failures | `AllowedOrigins` does not include the requesting origin — update `CorsSettings` |
| Missing `Username` / `Trace-Id` headers | Middleware pipeline order incorrect or observability adapter not registered |
