# Authentication Implementation

Camus API supports two authentication mechanisms: **JWT Bearer tokens** and **API Key authentication**. Both are
implemented directly in the API layer using ASP.NET Core 9 with credentials retrieved from a secure `ISecretProvider`.

> **📖 Parent Documentation:** [Main README](../README.md) | [Architecture Guide](architecture.md)

---

## JWT Authentication

JWT Bearer authentication provides secure token-based authentication using RSA256 asymmetric signing.

### Configuration

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

| Key | Type | Default | Description |
| --- | ---- | ------- | ----------- |
| `Issuer` | string | `https://auth.camus.com/` | Token issuer URL (max 200 chars, must be valid absolute URL) |
| `Audience` | string | `https://app.camus.com/` | Token audience URL (max 200 chars, must be valid absolute URL) |
| `ExpirationMinutes` | int | `60` | Token lifetime in minutes (range: 1–43200) |
| `RsaPrivateKeySecretName` | string | `RsaPrivateKeyPem` | Secret name for the RSA private key PEM (max 50 chars) |

Settings are validated at startup — invalid values cause a fail-fast `InvalidOperationException`.

### How to Get a Token

Post credentials to the `POST /api/v2/auth/authenticate` endpoint to receive a JWT token, then include it
in the `Authorization: Bearer` header on subsequent requests. The authenticate endpoint requires API Key
authentication (via the `Api-Key` header) since no JWT exists yet at that point. Both
`POST /api/v2/auth/authenticate` and `POST /api/v2/auth/generate-token` require an `Idempotency-Key`
header; requests without it receive HTTP 400. See the Swagger UI at `/swagger` for the complete
request/response specification.

### JWT Claims

JWT tokens include standard claims for user identification and token metadata; permission claims are included
based on the user's assigned permissions:

- `sub` — User ID
- `unique_name` — Username
- `jti` — Unique token identifier (for revocation)
- `iat` — Issued at timestamp
- `iss` — Issuer (from `JwtSettings.Issuer`)
- `aud` — Audience (from `JwtSettings.Audience`)
- `exp` — Expiration (from `JwtSettings.ExpirationMinutes`)
- `permission` — One claim per assigned permission

### Endpoint Protection

Use `[Authorize]` attribute on controllers or actions to require JWT authentication.
Use `[AllowAnonymous]` to opt out specific endpoints. Configure authorization policies
via `AddAuthorizationPolicies()` in `Program.cs`.

### Security Best Practices

- All secrets and signing keys are managed via DI and `ISecretProvider`
- Always use HTTPS in production
- Tokens expire after the configured time — consider short-lived tokens
- Change credentials in production and never store secrets in code
- Use RSA 2048-bit or higher for JWT signing keys

---

## API Key Authentication

API Key authentication provides simple header-based authentication for service-to-service communication.
Include the API key in the `Api-Key` request header.

### Configuration

In `appsettings.json`:

```json
{
  "ApiKeySettings": {
    "ApiKeySecretName": "XApiKey"
  }
}
```

| Key | Type | Default | Description |
| --- | ---- | ------- | ----------- |
| `ApiKeySecretName` | string | `XApiKey` | Secret name for the API key value retrieved from `ISecretProvider` |

The actual API key value is loaded at runtime from the configured secret store (Dapr, environment, etc.).
Settings are validated at startup — an empty secret name causes a fail-fast `InvalidOperationException`.

### How It Works

1. Client sends request with `Api-Key: <value>` header
2. `ApiKeyAuthenticationHandler` extracts the header value
3. Compares against the secret loaded from `ISecretProvider`
4. On success, creates a `ClaimsPrincipal` with username `ApiKeyUser` and a deterministic user ID
5. On failure, returns `401 Unauthorized`

### Security Best Practices

- Rotate API keys regularly
- Use different keys per environment
- Monitor API key usage for anomalies
- Transmit only over HTTPS

---

## Error Responses

- 401 Unauthorized: Token missing/expired/invalid or invalid credentials
- 403 Forbidden: Valid token, insufficient permissions

---

## Authentication Architecture

- Token generation is delegated to `AuthService` via the `AuthController`
- Credentials and signing keys are injected via DI
- API versioning and observability are integrated (trace context is enriched with trace ID and span ID)
- Both JWT and API Key authentication are registered in the API layer via `AddJwtAuthentication()` and
  `AddApiKeyAuthentication()` extension methods in `Program.cs`

---

## Choosing Authentication Method

- **JWT**: User authentication, mobile apps, SPAs, fine-grained permissions
- **API Key**: Service-to-service, webhooks, legacy systems, CLI tools
- **Both**: Endpoints can accept either authentication method using `[Authorize]` with multiple schemes
