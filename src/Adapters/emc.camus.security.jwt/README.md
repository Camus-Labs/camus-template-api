# emc.camus.security.jwt

JWT (JSON Web Token) authentication adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../README.md) |
[Authentication Guide](../../../docs/authentication.md)

---

## 📋 Overview

This adapter provides JWT Bearer authentication with RSA256 signing, enabling secure token-based authentication for your
API. It integrates with the Application layer's `ISecretProvider` for secure key management.

---

## ✨ Features

- 🔐 **JWT Bearer Authentication** - Industry-standard token authentication
- 🔑 **RSA256 Signing** - Asymmetric cryptography for token security
- 🎯 **Claims-Based Identity** - Flexible authorization with claims
- 🔄 **Interface-Based Design** - Depends on `ISecretProvider` from Application layer
- ⚙️ **Configurable** - Settings via `appsettings.json`
- 🚀 **ASP.NET Core Integration** - Seamless middleware integration

---

## 🚀 Usage

### 1. Register in Program.cs

Register the secret provider first (`builder.AddDaprSecrets()`), then call
`builder.AddJwtAuthentication()` to add JWT Bearer authentication. Enable the standard
authentication/authorization middleware.

See `JwtSetupExtensions` in this adapter for the full registration API.

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "https://auth.camus.com/",
    "Audience": "https://app.camus.com/",
    "ExpirationMinutes": 60
  }
}
```

### 3. Required Secrets

The adapter retrieves the RSA private key from your `ISecretProvider`:

**Development** (`src/Infrastructure/dapr/secrets.json`):

```json
{
  "RsaPrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
}
```

> **📖 Secret Management:** See [Dapr Secrets Adapter](../emc.camus.secrets.dapr/README.md) for
secret provider configuration.

---

## 🔐 Token Generation

### Using IJwtTokenGenerator

The adapter provides `IJwtTokenGenerator` for token generation. Inject it into your authentication
controller along with `ISecretProvider`. Validate credentials against secrets, build a
`GenerateTokenCommand` with desired claims, and call `GenerateToken(command)` to produce a
`GenerateTokenResult` containing the token string and expiration.

**Key Points:**

- ✅ Inject `IJwtTokenGenerator` via constructor
- ✅ Token generation is handled by the adapter
- ✅ Add custom claims as needed (roles, permissions, etc.)
- ✅ Returns `GenerateTokenResult` with token and expiration

See `JwtSetupExtensions` and `IJwtTokenGenerator` in this adapter for API details, and the auth
controller in `src/Api/emc.camus.api/Controllers/` for the wiring example.

---

## 🎯 Protecting Endpoints

### Require JWT Authentication

Apply `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` to controllers
or actions that require JWT authentication. Use `[Authorize(Roles = "Admin")]` for role-based
access or define custom policies for fine-grained authorization.

### Support Multiple Authentication Schemes

To accept either JWT or API Key on an endpoint, list both scheme names in the `[Authorize]`
attribute's `AuthenticationSchemes` parameter.

See controller source files in `src/Api/emc.camus.api/Controllers/` for examples.

---

## 🔑 JWT Claims

Standard claims included in tokens:

| Claim | Type | Description |
| ----- | ---- | ----------- |
| `sub` | Subject | User ID or identifier |
| `unique_name` | Username | User's display name |
| `jti` | JWT ID | Unique token identifier (GUID) |
| `iat` | Issued At | Token creation timestamp |
| `role` | Role | User roles (e.g., "User", "Admin") |
| `exp` | Expiration | Token expiration timestamp |
| `iss` | Issuer | Token issuer (from config) |
| `aud` | Audience | Intended audience (from config) |

**Access Claims in Code:**

Use `User.FindFirst(ClaimTypes.NameIdentifier)` for user ID,
`User.FindAll(ClaimTypes.Role)` for roles, and `User.FindFirst("unique_name")` for username.

---

## 🧪 Example Usage

### Get Token

```bash
POST /api/v2/auth/token
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
  "expiresOn": "2026-02-08T23:55:05.123Z"
}
```

### Use Token

```bash
GET /api/v1/products
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📊 Observability

### Metrics

The adapter exports OpenTelemetry metrics for monitoring authentication failures:

#### `jwt_authentication_failures_total`

**Type:** Counter  
**Unit:** requests  
**Description:** Total number of JWT authentication failures

**Dimensions:**

- `failure_reason` - The specific error code:
  - `token_expired` - JWT token has expired
  - `invalid_signature` - Token signature validation failed (potential tampering)
  - `invalid_issuer` - Token issuer doesn't match configuration
  - `invalid_audience` - Token audience doesn't match configuration
  - `invalid_token` - Token is malformed or cannot be parsed
  - `authentication_required` - No authentication provided
  - `forbidden` - Valid token but insufficient permissions
- `endpoint` - The API endpoint path that was accessed

**Use Cases:**

- **Attack Detection**: Spike in `invalid_signature` indicates potential token tampering attempts
- **Misconfiguration**: Sustained `invalid_issuer` or `invalid_audience` errors indicate
  configuration mismatch
- **User Experience**: High rate of `token_expired` may indicate expiration window is too short
- **Security Monitoring**: Track which endpoints are being targeted by unauthorized access
  attempts

**Example Queries:**

| Query Purpose | Description |
| ------------- | ----------- |
| Total failures (1h) | Sum of `jwt_authentication_failures_total` increase over the last hour |
| Failure rate by reason | Rate of `jwt_authentication_failures_total` grouped by `failure_reason` |
| Endpoints under attack | Top 5 endpoints by `invalid_signature` failure rate |

### Error Handling

Authentication failures are handled via exceptions with error codes in `exception.Data[ErrorCodes.ErrorCodeKey]`. The
global exception handler logs errors and returns RFC 7807 Problem Details responses.

---

## Integration

The adapter registers via the extension methods in `JwtSetupExtensions.cs`:

- **`builder.AddJwtAuthentication()`** — Reads `JwtSettings` from configuration, resolves RSA
  keys from the secret provider, and registers JWT bearer authentication and
  token-generation services.

Apply the `[Authorize]` attribute (with the appropriate authentication scheme) to controllers or
actions that require JWT authentication.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `invalid_signature` on all tokens | RSA private key mismatch or key not loaded from secret store |
| `token_expired` immediately | Server clock skew or `ExpirationMinutes` too low |
| `invalid_issuer` / `invalid_audience` | `JwtSettings:Issuer` or `Audience` doesn't match the token’s claims |
| 401 with no error code | `[Authorize]` attribute missing `AuthenticationSchemes` parameter |
| Secret provider failure at startup | `AddDaprSecrets()` not called before `AddJwtAuthentication()` |

---

## Error Codes

The adapter surfaces machine-readable error codes in HTTP responses for client error handling:

### JWT Error Codes

| Error Code | HTTP Status | Description | Client Action |
| ---------- | ----------- | ----------- | ------------- |
| `token_expired` | 401 | JWT token has expired | Refresh token or redirect to login |
| `invalid_token` | 401 | Token is malformed or cannot be parsed | Request new token |
| `invalid_signature` | 401 | Token signature validation failed | Token may be tampered, request new token |
| `invalid_issuer` | 401 | Token issuer doesn't match expected value | Check token source |
| `invalid_audience` | 401 | Token audience doesn't match expected value | Token not intended for this API |
| `authentication_required` | 401 | No authentication credentials provided | Provide JWT token in Authorization header |
| `invalid_credentials` | 401 | Credentials are incorrect | Check access key and secret |
| `forbidden` | 403 | Valid authentication but insufficient permissions | Request elevated access or different endpoint |

**Error Response Example:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "You are not authorized to access this resource.",
  "instance": "/api/v2/protected",
  "error": "token_expired"
}
```

**Client Implementation:**

Handle JWT error codes on the client side: refresh the token on `token_expired`, and clear
credentials and re-authenticate on `invalid_signature`. See `ErrorCodes.cs` for the complete
list of error codes.

> **📖 Error Codes Reference:** See [ErrorCodes.cs](../../Application/emc.camus.application/Common/ErrorCodes.cs)
for complete error code definitions.

---

## 🏗️ Architecture

### Separation of Concerns

```text
┌──────────────────────────────────────┐
│      Application Layer               │
│        ISecretProvider               │
└───────────────┬──────────────────────┘
                │ depends on
┌───────────────▼──────────────────────┐
│       Adapter Layer                  │
│  JwtAuthenticationSetupExtensions    │
│  (uses ISecretProvider for keys)    │
└───────────────┬──────────────────────┘
                │
┌───────────────▼──────────────────────┐
│   ASP.NET Core Authentication        │
│     JwtBearerHandler                 │
└──────────────────────────────────────┘
```

**Benefits:**

- Adapter doesn't know about Dapr, Azure Key Vault, etc.
- Easy to swap secret providers
- Testable with mocked `ISecretProvider`

---

## 🔒 Security Best Practices

### RSA Key Generation

Generate a 2048-bit RSA private key using `openssl genrsa`, extract the public key with
`openssl rsa -pubout`, and store the private key PEM in the secret provider. Never commit private
keys to source control.

### Production Recommendations

- ✅ Use RSA 2048-bit or higher keys
- ✅ Store keys in secure secret management (Azure Key Vault, AWS Secrets Manager)
- ✅ Rotate keys regularly
- ✅ Use short token expiration times (15-60 minutes)
- ✅ Implement refresh tokens for long-lived sessions
- ✅ Enable HTTPS in production
- ✅ Validate issuer and audience claims
- ❌ Never commit private keys to version control
- ❌ Never use symmetric keys (HS256) for distributed systems
- ❌ Never log JWT tokens

---

## 🧪 Testing

### Unit Tests

Mock `ISecretProvider` to return a test RSA private key, then use the adapter's token generation
API to verify token creation.

### Integration Tests

Use `WebApplicationFactory` to create a test client, obtain a token from the auth endpoint, set
the `Authorization: Bearer` header, and assert on protected endpoints. See test projects in
`src/Test/` for integration test patterns.

---

## 🔗 Related Documentation

- **[API Key Authentication Adapter](../emc.camus.security.apikey/README.md)** - Alternative authentication method
- **[Authentication Guide](../../../docs/authentication.md)** - Complete authentication overview
- **[Secrets Adapter](../emc.camus.secrets.dapr/README.md)** - Secret management
- **[Architecture Guide](../../../docs/architecture.md)** - Security architecture
- **[JWT.io](https://jwt.io/)** - JWT debugger and documentation

---

## 📦 Dependencies

- `emc.camus.application` - Application interfaces (`ISecretProvider`)
- Microsoft.AspNetCore.Authentication.JwtBearer
- System.IdentityModel.Tokens.Jwt
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection

---

## ⚙️ Advanced Configuration

### Custom Token Validation

Configure `TokenValidationParameters` in `JwtSetupExtensions` to control issuer, audience,
lifetime, and signing key validation. Set `ClockSkew` to `TimeSpan.Zero` for strict expiration
checking.

### Custom Authorization Policies

After calling `builder.AddCamusAuthorization()`, add custom policies via
`builder.Services.AddAuthorization()` using `RequireRole()`, `RequireClaim()`, or custom
requirements. See `AuthorizationSetupExtensions` in the API layer for the base configuration.

## Design Principles

✅ **Separation of Concerns** - Authentication and authorization split  
✅ **Loose Coupling** - Depends only on abstractions  
✅ **Reusability** - Can be used across multiple APIs  
✅ **Testability** - Easy to mock `ISecretProvider` for tests  
✅ **Maintainability** - All security logic in one place  
✅ **Extensibility** - Ready for complex authorization scenarios  
✅ **Maintainability** - All authentication logic in one place
