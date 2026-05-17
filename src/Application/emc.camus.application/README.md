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
- ✅ **Attributes** for declarative behavior (e.g., `[RateLimit]`)
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

### `Auth/`

Authentication-related contracts and services:

- **`IAuthService`** - Interface for the authentication application service
- **`AuthService`** - Orchestrates user authentication, token generation, listing, and revocation
- **`ITokenGenerator`** - Interface for JWT token generation (implemented by `emc.camus.security.jwt`)
- **`IUserRepository`** - Repository contract for user credential validation and retrieval
- **`IGeneratedTokenRepository`** - Repository contract for managing generated tokens
- **`GenerateTokenResult`** - Token generation result model
- **`GeneratedTokenFilter`** - Filter criteria record for generated token queries
- **`GeneratedTokenSortField`** - Enum for sortable fields (TokenUsername, ExpiresOn, CreatedAt, RevokedAt)
- **`GeneratedTokenSortParams`** - Encapsulates optional sort field and direction for token queries
- **`AuthenticationSchemes`** - Authentication scheme name constants (`Bearer`, `ApiKey`)
- **`ITokenRevocationCache`** - Interface for token revocation cache (implemented by `emc.camus.cache.inmemory`)

### `Observability/`

Telemetry and monitoring contracts:

- **`IActivitySourceWrapper`** - Interface for distributed tracing (implemented by `emc.camus.observability.otel`)
- **`OperationType`** - Enum for operation types in telemetry (Read, Auth, Create, etc.)
- **`MeterNames`** - OpenTelemetry meter name suffix constants (Application, Business, Security, Infrastructure)

### `RateLimiting/`

Rate limiting contracts:

- **`RateLimitAttribute`** - Attribute for applying rate limit policies to controllers/actions
- **`RateLimitPolicies`** - Policy name constants (`Default`, `Strict`, `Relaxed`)

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

### `Configurations/`

Configuration types used by persistence and infrastructure:

- **`DataPersistenceSettings`** — Global persistence provider selection
  (`InMemory` or `PostgreSQL`)
- **`DatabaseSettings`** — PostgreSQL connection parameters
  (Host, Port, Database, UserSecretName, PasswordSecretName)
- **`PersistenceProvider`** — Enum: `InMemory`, `PostgreSQL`

### `Exceptions/`

Custom exceptions:

- **`DataConflictException`** - Exception thrown when a data conflict is detected (HTTP 409)
- **`RateLimitExceededException`** - Exception thrown when rate limits are exceeded

---

## 📦 Dependencies

The Application layer has **minimal dependencies** — only `System.Diagnostics.DiagnosticSource`.

**Dependency Rule:** Application layer must **never depend on infrastructure packages** (database, HTTP,
logging frameworks).

---

## 🔗 Implementations

Application interfaces are implemented in the following adapters:

| Interface | Implementation | Adapter Project |
| --------- | -------------- | --------------- |
| `ITokenGenerator` | `JwtTokenGenerator` | `emc.camus.security.jwt` |
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

- [JWT Authentication](../../Adapters/emc.camus.security.jwt/README.md)
- [Dapr Secrets](../../Adapters/emc.camus.secrets.dapr/README.md)
- [OpenTelemetry Observability](../../Adapters/emc.camus.observability.otel/README.md)
- [PostgreSQL Persistence](../../Adapters/emc.camus.persistence.postgresql/README.md)
- [In-Memory Persistence](../../Adapters/emc.camus.persistence.inmemory/README.md)

---

## 📖 Usage Examples

See `RateLimitAttribute` and `RateLimitPolicies` in the `RateLimiting` namespace for rate limit attribute
usage. See `AuthenticationSchemes` in the `Auth` namespace for available authentication scheme constants.

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

See [RateLimitPolicies.cs](RateLimiting/RateLimitPolicies.cs) for complete policy definitions:

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
- `RateLimit-Limit` - Max requests allowed
- `RateLimit-Reset` - Reset timestamp
- `RateLimit-Policy` - Applied policy name
- `RateLimit-Window` - Window duration
- `Idempotency-Key` - Idempotency key identification
- `Idempotency-Key-Status` - Response header indicating cache status (`hit` or `miss`)

---

## Configuration

The following configuration types are defined in the Application layer:

- **`DataPersistenceSettings`** — Selects the global persistence provider
  (`InMemory` or `PostgreSQL`). Section name: `DataPersistenceSettings`.
- **`DatabaseSettings`** — PostgreSQL connection parameters (Host, Port, Database, UserSecretName,
  PasswordSecretName). Section name: `DatabaseSettings`.

Adapter projects that implement these interfaces provide their own additional configuration. See individual adapter
READMEs for details.

---

## Integration

Consuming projects reference `emc.camus.application` to access interface contracts, attributes, constants, and
exception types. The API layer wires concrete adapter implementations to these interfaces at startup via dependency
injection. See the extension methods in `src/Api/emc.camus.api/Extensions/` for the registration patterns.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `MissingMethodException` on interface call | Adapter project not referenced or DI registration missing |
| `RateLimitAttribute` has no effect | Rate limiting adapter not registered — call `builder.AddInMemoryRateLimiting(serviceName)` |
| `ErrorCodes` constant not found | Missing `using emc.camus.application.Common;` directive |

---

## 🧪 Testing

Application layer components are tested in `src/Test/emc.camus.application.test/`. Run via the VS Code
**test-unit** task.

Tests focus on:

- Attribute behavior (e.g., `RateLimitAttribute` validation)
- Exception properties and messages
- Constant value correctness
- *(Note: Interfaces are tested via their adapter implementations)*

---

## 📚 Related Documentation

- [Architecture Overview](../../../docs/architecture.md) - Layer responsibilities and dependency flow
- [Main README](../../../README.md) - Project overview and quick start
- [Rate Limiting Adapter](../../Adapters/emc.camus.ratelimiting.inmemory/README.md) - Rate limiting implementation
- [JWT Security Adapter](../../Adapters/emc.camus.security.jwt/README.md) - JWT authentication implementation
- [OpenTelemetry Adapter](../../Adapters/emc.camus.observability.otel/README.md) - Observability implementation
