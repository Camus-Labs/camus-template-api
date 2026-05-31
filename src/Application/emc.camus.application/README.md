# Application Layer - Shared Contracts

> **📖 Parent Documentation:** [Main README](../../../README.md) |
[Architecture Guide](../../../docs/architecture.md)

The Application layer defines **shared contracts** (interfaces, attributes, exceptions, and constants)
consumed by the API layer and infrastructure adapters. It also contains concrete application services
(`AuthService`, `ApiInfoService`) that orchestrate repository and adapter calls.

---

## 📋 Purpose

The Application layer serves as the **contracts layer** between API/Adapters and Domain:

- ✅ **Interfaces** consumed by API layer or multiple adapters
- ✅ **Exceptions** for standardized error handling
- ✅ **Constants** for application-wide values (error codes, headers, policies)

**What does NOT belong here:**

- ❌ Implementations (belong in Adapters)
- ❌ Infrastructure concerns (databases, HTTP, logging)
- ❌ Business logic (belongs in Domain)

---

## 📁 Namespace Structure

### `ApiInfo/`

API information contracts and services:

- **`IApiInfoService`** - Interface for the API information application service
- **`ApiInfoService`** - Retrieves and provides API version information
- **`IApiInfoRepository`** - Repository contract for retrieving API information
- **`ApiInfoFilter`** - Filter record for querying API information by version (normalizes and validates the version string)
- **`ApiInfoDetailView`** - Detail view record containing API version, status, and available features

### `Auth/`

Authentication-related contracts and services:

- **`IAuthService`** - Interface for the authentication application service
- **`AuthService`** - Orchestrates user authentication, token generation, listing, and revocation
- **`ITokenGenerator`** - Interface for JWT token generation (implemented by `emc.camus.api`)
- **`IUserRepository`** - Repository contract for user credential validation and retrieval
- **`IGeneratedTokenRepository`** - Repository contract for managing generated tokens
- **`AuthenticateUserCommand`** - Command record for user authentication requests
- **`GenerateTokenCommand`** - Command record for token generation requests
- **`RevokeTokenCommand`** - Command record for token revocation requests
- **`AuthenticateUserResult`** - Result of a successful user authentication operation
- **`GenerateTokenResult`** - Token generation result model
- **`GeneratedTokenSummaryView`** - Summary view record for generated token listings
- **`GeneratedTokenFilter`** - Filter criteria record for generated token queries
- **`GeneratedTokenSortField`** - Enum for sortable fields (TokenUsername, ExpiresOn, CreatedAt, RevokedAt)
- **`Permissions`** - Permission name constants for authorization
- **`AuthMappingExtensions`** - Extension methods mapping domain entities to application-layer views
- **`ITokenRevocationCache`** - Interface for token revocation cache (implemented by `emc.camus.cache.inmemory`)

### `Observability/`

Telemetry and monitoring contracts:

- **`IActivitySourceWrapper`** - Interface for distributed tracing (implemented by `emc.camus.observability.otel`)
- **`OperationType`** - Enum for operation types in telemetry (Read, Auth, Create, etc.)
- **`MeterNames`** - OpenTelemetry meter name suffix constants (Application, Business, Security, Infrastructure, ErrorHandling)

### `Secrets/`

Secret management contracts:

- **`ISecretProvider`** - Interface for secret retrieval (implemented by `emc.camus.secrets.dapr`)

### `Idempotency/`

Idempotency response caching contracts:

- **`IIdempotencyResponseCache`** - Interface for storing and retrieving cached idempotent responses
- **`CachedResponse`** - Sealed class holding cached status code, body, and body hash
- **`IdempotencyKeyStatuses`** - Constants for `Idempotency-Key-Status` header values (`hit`, `miss`)

### `Common/`

Application-wide constants and shared contracts:

- **`IUnitOfWork`** - Abstracts transactional boundaries for application services
- **`IActionAuditRepository`** - Repository contract for managing action audit logs
- **`IUserContext`** - Interface for accessing current user information
- **`PagedResult<T>`** - Generic paged result wrapper
- **`PaginationParams`** - Pagination parameters model
- **`ErrorCodes`** - Standardized error codes for API responses (`bad_request`, `unauthorized`,
  `rate_limit_exceeded`, etc.)
- **`Headers`** - Custom HTTP header name constants (`Api-Key`, `Trace-Id`, `Idempotency-Key`, rate limit
  headers)
- **`MediaTypes`** - Custom media type constants (`application/problem+json`)
- **`SortDirection`** - Enum for sort direction (Asc, Desc)
- **`SortParams<T>`** - Generic record for sort field and direction parameters
- **`HealthCheckTags`** - Health check tag constants for endpoint predicates
- **`IServiceInitializer`** - Contract for services requiring initialization at startup

### `Configurations/`

Configuration types used by persistence and infrastructure:

- **`DataPersistenceSettings`** — Global persistence provider selection
  (`InMemory` or `PostgreSQL`)
- **`DatabaseSettings`** — PostgreSQL connection parameters
  (Host, Port, Database, UserSecretName, PasswordSecretName, AdditionalParameters)
- **`PersistenceProvider`** — Enum: `InMemory`, `PostgreSQL`

### `Exceptions/`

Custom exceptions:

- **`DataConflictException`** - Exception thrown when a data conflict is detected (HTTP 409)

---

## 📦 Dependencies

The Application layer has **minimal dependencies** — only a project reference to the Domain layer.

See [Architecture Guide](../../../docs/architecture.md) for dependency constraints between layers.

---

## 🔗 Implementations

Application interfaces are implemented in the following adapters:

| Interface | Implementation | Adapter Project |
| --------- | -------------- | --------------- |
| `ITokenGenerator` | `JwtTokenGenerator` | `emc.camus.api` |
| `ISecretProvider` | `DaprSecretProvider` | `emc.camus.secrets.dapr` |
| `IActivitySourceWrapper` | `ActivitySourceWrapper` | `emc.camus.observability.otel` |
| `IUserRepository` | `UserRepository`, `UserRepository` | `emc.camus.persistence.postgresql`, `emc.camus.persistence.inmemory` |
| `IApiInfoRepository` | `ApiInfoRepository`, `ApiInfoRepository` | `emc.camus.persistence.postgresql`, `emc.camus.persistence.inmemory` |
| `IActionAuditRepository` | `ActionAuditRepository`, `ActionAuditRepository` | `emc.camus.persistence.postgresql`, `emc.camus.persistence.inmemory` |
| `IGeneratedTokenRepository` | `GeneratedTokenRepository` | `emc.camus.persistence.postgresql` |
| `IUnitOfWork` | `UnitOfWork`, `UnitOfWork` | `emc.camus.persistence.postgresql`, `emc.camus.persistence.inmemory` |
| `IIdempotencyResponseCache` | `IdempotencyResponseCache` | `emc.camus.cache.inmemory` |
| `ITokenRevocationCache` | `TokenRevocationCache` | `emc.camus.cache.inmemory` |

See individual adapter READMEs for implementation details:

- [Dapr Secrets](../../Adapters/emc.camus.secrets.dapr/README.md)
- [OpenTelemetry Observability](../../Adapters/emc.camus.observability.otel/README.md)
- [PostgreSQL Persistence](../../Adapters/emc.camus.persistence.postgresql/README.md)
- [In-Memory Persistence](../../Adapters/emc.camus.persistence.inmemory/README.md)

---

## 📖 Usage Examples

See `AuthenticationSchemes` in `emc.camus.api` `Configurations` namespace for available authentication
scheme constants. See `RateLimitAttribute` and `RateLimitPolicies` in the API layer `Filters/` and
`Configurations/` namespaces for rate limit attribute usage.

---

## 📊 Constants Reference

### Error Codes

- `bad_request` - 400 Bad Request
- `authentication_required` - 401 No authentication provided
- `apikey_authentication_required` - 401 API Key missing
- `unauthorized` - 401 General unauthorized
- `invalid_credentials` - 401 Credentials invalid
- `auth_invalid_credentials` - 401 Username/password authentication failed
- `apikey_invalid_credentials` - 401 API Key invalid
- `forbidden` - 403 Forbidden
- `not_found` - 404 Not Found
- `data_conflict` - 409 Conflict
- `domain_rule_violation` - 422 Domain business rule violated
- `rate_limit_exceeded` - 429 Too Many Requests
- `request_timeout` - 504 Request cancelled by timeout or client disconnect
- `internal_server_error` - 500 Server error
- `idempotency_key_missing` - 400 Idempotency-Key header missing on a decorated endpoint
- `idempotency_key_invalid` - 400 Idempotency-Key header value empty or exceeds max length
- `idempotency_body_conflict` - 409 Idempotency key reused with different request body
- JWT-specific: `jwt_authentication_required`, `jwt_invalid_credentials`, `jwt_token_expired`,
  `jwt_invalid_token`, `jwt_invalid_signature`, `jwt_invalid_issuer`, `jwt_invalid_audience`,
  `jwt_token_revoked`

### Rate Limit Policies

Policy definitions are in the API layer at `src/Api/emc.camus.api/Configurations/RateLimitPolicies.cs`:

- `default` - Standard rate limit
- `strict` - For sensitive endpoints
- `relaxed` - For high-throughput operations

### Meter Names (OpenTelemetry)

- `` (empty) - Base application meter
- `.business` - Business domain metrics
- `.security` - Security metrics (auth, rate limiting)
- `.infrastructure` - Infrastructure metrics (DB, cache, external APIs)
- `.errorhandling` - Error handling and exception tracking metrics

### Custom Headers

- `Api-Key` - API Key authentication
- `Trace-Id` - Distributed tracing correlation
- `Username` - Authenticated user identification
- `Idempotency-Key` - Idempotency key for request deduplication
- `Idempotency-Key-Status` - Cache hit/miss indicator
- `RateLimit-Limit` - Max requests allowed
- `RateLimit-Reset` - Reset timestamp
- `RateLimit-Policy` - Applied policy name
- `RateLimit-Window` - Window duration

---

## ⚙️ Configuration

This project defines configuration types consumed by adapters — it does not require its own
configuration. The settings types are:

- **`DataPersistenceSettings`** — bound from section `DataPersistenceSettings` to select the active provider
- **`DatabaseSettings`** — bound from section `DatabaseSettings` with PostgreSQL connection parameters

See [PostgreSQL Persistence](../../Adapters/emc.camus.persistence.postgresql/README.md) for how these
settings are consumed.

---

## 🔌 Integration

Consuming layers reference this project to access contracts:

- **API layer** — registers application services (`AuthService`, `ApiInfoService`) via DI and depends
  on interfaces for middleware (e.g., `IUserContext`, `IIdempotencyResponseCache`)
- **Adapter projects** — implement interfaces defined here (`ITokenGenerator`, `ISecretProvider`,
  `IUserRepository`, etc.) and register themselves in the DI container

Dependency direction: `API/Adapters → Application → Domain`

---

## 🛠️ Troubleshooting

| Symptom | Cause | Fix |
| ------- | ----- | --- |
| `Unable to resolve service for type 'IXxx'` | Adapter not registered in DI | Ensure the adapter's `AddXxx()` extension is called in `Program.cs` |
| `InvalidOperationException` on settings validation | Missing or invalid configuration section | Verify `appsettings.json` contains the required section with valid values |
| Sort field not recognized | Enum value mismatch between API model and `GeneratedTokenSortField` | Confirm the API maps to a valid `GeneratedTokenSortField` value |

---

## 🧪 Testing

Application layer components are tested in `src/Test/emc.camus.application.test/`. Run via the VS Code
**test-unit** task.

Tests focus on:

- Exception properties and messages
- Constant value correctness
- *(Note: Interfaces are tested via their adapter implementations)*

---

## 📚 Related Documentation

- [Architecture Overview](../../../docs/architecture.md) - Layer responsibilities and dependency flow
- [Main README](../../../README.md) - Project overview and quick start
- [API Layer](../../Api/emc.camus.api/README.md) - Rate limiting, JWT, and API Key implementations
- [OpenTelemetry Adapter](../../Adapters/emc.camus.observability.otel/README.md) - Observability implementation
