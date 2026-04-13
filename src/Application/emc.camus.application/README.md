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
- **`AuthenticationSchemes`** - Authentication scheme name constants (`Bearer`, `ApiKey`)

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

### `Common/`

Application-wide constants and shared contracts:

- **`IUnitOfWork`** - Abstracts transactional boundaries for application services
- **`IActionAuditRepository`** - Repository contract for managing action audit logs
- **`IUserContext`** - Interface for accessing current user information
- **`PagedResult<T>`** - Generic paged result wrapper
- **`PaginationParams`** - Pagination parameters model
- **`ErrorCodes`** - Standardized error codes for API responses (`bad_request`, `unauthorized`,
  `rate_limit_exceeded`, etc.)
- **`Headers`** - Custom HTTP header name constants (`X-Api-Key`, `X-Trace-Id`, rate limit
  headers)
- **`MediaTypes`** - Custom media type constants (`application/problem+json`)

### `Configurations/`

Configuration types used by persistence and infrastructure:

- **`DataPersistenceSettings`** — Global persistence provider selection
  (`InMemory` or `PostgreSQL`)
- **`DatabaseSettings`** — PostgreSQL connection parameters
  (Host, Port, Database, UserSecretName, PasswordSecretName)
- **`PersistenceProvider`** — Enum: `InMemory`, `PostgreSQL`

### `Exceptions/`

Custom exceptions:

- **`RateLimitExceededException`** - Exception thrown when rate limits are exceeded

---

## 🔌 Interface Placement Decision Framework

**When to place an interface in Application layer:**

| Consumer | Decision | Reasoning |
| -------- | -------- | --------- |
| **API layer** | ✅ Keep in Application | Prevents API from depending on adapter implementations |
| **Multiple adapters** | ✅ Keep in Application | Shared contract across multiple implementations |
| **Single adapter only** | ⚠️ Move to adapter | No need for abstraction if only one consumer |
| **Nobody (future use)** | ❌ Remove it | YAGNI - don't build abstractions until needed |

**Examples from this project:**

- **`ITokenGenerator`** - ✅ In Application (consumed by API `AuthController`)
- **`ISecretProvider`** - ✅ In Application (consumed by multiple adapters: JWT, ApiKey)
- **`IActivitySourceWrapper`** - ✅ In Application (consumed by API middleware and potentially multiple adapters)

---

## 📦 Dependencies

The Application layer has **minimal dependencies**:

```xml
<ItemGroup>
  <PackageReference Include="System.Diagnostics.DiagnosticSource" />
</ItemGroup>
```

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

See individual adapter READMEs for implementation details:

- [JWT Authentication](../../Adapters/emc.camus.security.jwt/README.md)
- [Dapr Secrets](../../Adapters/emc.camus.secrets.dapr/README.md)
- [OpenTelemetry Observability](../../Adapters/emc.camus.observability.otel/README.md)
- [PostgreSQL Persistence](../../Adapters/emc.camus.persistence.postgresql/README.md)
- [In-Memory Persistence](../../Adapters/emc.camus.persistence.inmemory/README.md)

---

## 📖 Usage Examples

### Using RateLimit Attribute

Apply `RateLimit` attribute to controllers:

- `[RateLimit(RateLimitPolicies.Strict)]` for sensitive endpoints.
- `[RateLimit(RateLimitPolicies.Relaxed)]` for high-throughput operations.

See `RateLimitAttribute` and `RateLimitPolicies` in the `RateLimiting` namespace for available options.

### Using Authentication Schemes

Apply `[Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer)]` to controllers or actions requiring
JWT authentication. See `AuthenticationSchemes` in the `Auth` namespace for available scheme constants.

---

## 📊 Constants Reference

### Error Codes

- `bad_request` - 400 Bad Request
- `authentication_required` - 401 No authentication provided
- `unauthorized` - 401 General unauthorized
- `invalid_credentials` - 401 Credentials invalid
- `forbidden` - 403 Forbidden
- `rate_limit_exceeded` - 429 Too Many Requests
- `internal_server_error` - 500 Server error
- JWT-specific: `token_expired`, `invalid_token`, `invalid_signature`

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

### Custom Headers

- `X-Api-Key` - API Key authentication
- `X-Trace-Id` - Distributed tracing correlation
- `RateLimit-Limit` - Max requests allowed
- `RateLimit-Reset` - Reset timestamp
- `Retry-After` - Retry after seconds
- `X-RateLimit-Policy` - Applied policy name
- `X-RateLimit-Window` - Window duration

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

Application layer components are tested in `src/Test/emc.camus.application.test/`:

```bash
# Run Application layer tests
dotnet test src/Test/emc.camus.application.test/
```

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
