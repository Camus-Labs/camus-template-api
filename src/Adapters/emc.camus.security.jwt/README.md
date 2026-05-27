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

Register the secret provider first (`builder.AddDaprSecrets()`), then call `builder.AddJwtAuthentication()` to
add JWT Bearer authentication. Enable the standard authentication/authorization middleware.

See `JwtSetupExtensions` in this adapter for the full registration API.

See [Configuration](#️-configuration) below for settings and secret requirements.

---

## ⚙️ Configuration

### Settings

In `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "https://auth.camus.com/",
    "Audience": "https://app.camus.com/",
    "ExpirationMinutes": 60,
    "RsaPrivateKeySecretName": "RsaPrivateKeyPem"
  }
}
```

### Required Secrets

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

### Using ITokenGenerator

The adapter registers `ITokenGenerator` in DI for use by the Application layer's `IAuthService`.
Controllers inject `IAuthService` (not `ITokenGenerator` directly). The token generator accepts a user ID,
username, and optional additional claims, returning an `AuthToken` with the token string and expiration.

**Key Points:**

- ✅ Controllers inject `IAuthService`, which internally uses `ITokenGenerator`
- ✅ Token generation is handled by the adapter via the `GenerateToken` method, which accepts a user ID,
  username, and optional additional claims
- ✅ Add custom claims as needed (roles, permissions, etc.)
- ✅ Returns `AuthToken` with token and expiration

See `JwtSetupExtensions` and `ITokenGenerator` in this adapter for API details, and the auth controller in
`src/Api/emc.camus.api/Controllers/` for the wiring example.

---

## 🎯 Protecting Endpoints

### Require JWT Authentication

Apply an Authorize attribute specifying the JWT bearer authentication scheme to controllers or actions
that require JWT authentication. Use role-based authorization attributes for access control or define custom
policies for fine-grained authorization.

### Support Multiple Authentication Schemes

To accept either JWT or API Key on an endpoint, list both scheme names in the Authorize attribute's
authentication schemes parameter.

See controller source files in `src/Api/emc.camus.api/Controllers/` for examples.

---

## 🔑 JWT Claims

Standard claims included in every token:

| Claim | Type | Description |
| ----- | ---- | ----------- |
| `sub` | Subject | User ID or identifier |
| `unique_name` | Username | User's display name |
| `jti` | JWT ID | Unique token identifier (GUID) |
| `iat` | Issued At | Token creation timestamp |
| `exp` | Expiration | Token expiration timestamp |
| `iss` | Issuer | Token issuer (from config) |
| `aud` | Audience | Intended audience (from config) |

Optional claims (included only when supplied via `additionalClaims`):

| Claim | Type | Description |
| ----- | ---- | ----------- |
| `permission` | Permission | User permissions (e.g., "api.read", "api.write") |

See controller source files in `src/Api/emc.camus.api/Controllers/` for claims access patterns.

---

## 🧪 Example Usage

Authenticate by posting credentials to the auth endpoint, then include the returned token in the `Authorization: Bearer`
header on subsequent requests. See the Swagger documentation for the complete API specification.

---

## 📊 Observability

### Error Handling

Authentication failures are handled via exceptions. The global exception handler in the API layer logs errors,
increments `error_responses_total` (via `ErrorMetrics`), and returns RFC 7807 Problem Details responses with
error codes defined in `ErrorCodes.cs` (e.g., `jwt_token_expired`, `jwt_invalid_signature`,
`jwt_invalid_issuer`, `jwt_invalid_audience`, `jwt_authentication_required`).

---

## Integration

The adapter registers via the extension methods in `JwtSetupExtensions.cs`:

- **`builder.AddJwtAuthentication()`** — Reads `JwtSettings` from configuration, resolves RSA keys
  from the secret provider, and registers JWT bearer authentication and token-generation services.

**Required Dependencies:** An `ITokenRevocationCache` implementation must be registered in the DI container
before the host starts accepting requests. Token validation checks the revocation cache on every
authenticated request.

Apply the `[Authorize]` attribute (with the appropriate authentication scheme) to controllers or actions that
require JWT authentication.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `jwt_invalid_signature` on all tokens | RSA private key mismatch or key not loaded from secret store |
| `jwt_token_expired` immediately | Server clock skew or `ExpirationMinutes` too low |
| `jwt_invalid_issuer` / `jwt_invalid_audience` | `JwtSettings:Issuer` or `Audience` doesn't match the token's claims |
| 401 with no error code | `[Authorize]` attribute missing `AuthenticationSchemes` parameter |
| Secret provider failure at startup | `AddDaprSecrets()` not called before `AddJwtAuthentication()` |

---

**Client Implementation:**

Handle JWT error codes on the client side: re-authenticate to obtain a new token on `jwt_token_expired`, and clear
credentials and re-authenticate on `jwt_invalid_signature`. See `ErrorCodes.cs` for the complete list of error codes.

> **📖 Error Codes Reference:** See [ErrorCodes.cs](../../Application/emc.camus.application/Common/ErrorCodes.cs)
for complete error code definitions.

---

## 🏗️ Architecture

### Separation of Concerns

```text
┌──────────────────────────────────────┐
│      Application Layer               │
│        ISecretProvider               │
└───────────────▲──────────────────────┘
                │ depends on
┌───────────────┴──────────────────────┐
│       Adapter Layer                  │
│  JwtSetupExtensions                  │
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

Generate a 2048-bit RSA private key, extract the corresponding public key, and store the private key PEM
in the secret provider. Use a standard cryptographic tool such as OpenSSL for key generation — refer to the
[OpenSSL documentation](https://www.openssl.org/docs/) for exact commands. Never commit private keys to
source control.

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

Mock `ISecretProvider` to return a test RSA private key, then use the adapter's token generation API to verify
token creation.

### Integration Tests

Use `WebApplicationFactory` to create a test client, obtain a token from the auth endpoint, set the `Authorization:
Bearer` header, and assert on protected endpoints. See test projects in `src/Test/` for integration test patterns.

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

Configure `TokenValidationParameters` in `JwtSetupExtensions` to control issuer, audience, lifetime, and signing
key validation. Set `ClockSkew` to `TimeSpan.Zero` for strict expiration checking.

### Custom Authorization Policies

After calling `builder.AddAuthorizationPolicies()`, add custom policies via `builder.Services.AddAuthorization()`
using `RequireRole()`, `RequireClaim()`, or custom requirements. See `AuthorizationSetupExtensions` in the API
layer for the base configuration.

## Design Principles

- ✅ **Separation of Concerns** - Authentication and authorization split
- ✅ **Loose Coupling** - Depends only on abstractions
- ✅ **Reusability** - Can be used across multiple APIs
- ✅ **Testability** - Easy to mock `ISecretProvider` for tests
- ✅ **Maintainability** - All security logic in one place
- ✅ **Extensibility** - Ready for complex authorization scenarios
