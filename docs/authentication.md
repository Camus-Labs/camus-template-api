# Authentication Implementation

Camus API supports two authentication mechanisms: **JWT Bearer tokens** and **API Key authentication**. Both are implemented using ASP.NET Core 9 with credentials retrieved from a secure `ISecretProvider`.

## JWT Authentication

## Configuration

JWT settings are configured in `appsettings.json`:

```json
"JwtSettings": {
  "Issuer": "https://auth.camus.com/",
  "Audience": "https://app.camus.com/",
  "ExpirationMinutes": 120
}
```

- The signing key (RSA or symmetric) is registered in DI and not stored in config files.
- Access credentials (`AccessKey`, `AccessSecret`) are retrieved via `ISecretProvider`.

## How to Get a Token

**Endpoint:** `POST /api/v2/Auth/token`

**Request Body:**

```json
{
  "accessKey": "YOUR_ACCESS_KEY",
  "accessSecret": "YOUR_ACCESS_SECRET"
}
```

- Credentials are validated against the values from the secret provider.

**Response:**

```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresOn": "2026-01-15T23:55:05.123Z"
}
```

## Using the Token

Include the token in the `Authorization` header:

```http
Authorization: Bearer {your-token}
```

## Info Endpoints

- `GET /api/v1/Auth/info` — Public, no authentication required. Returns API info and version (from request context).
- `GET /api/v1/Auth/info-apikey` — Requires API Key authentication.
- `GET /api/v1/Auth/info-jwt` — Requires JWT authentication.
- All info endpoints log and tag the API version for observability.

## Claims in JWT

- `sub`: User ID (from accessKey)
- `unique_name`: Username
- `jti`: Unique token ID (GUID)
- `iat`: Issued at timestamp
- `role`: User roles (e.g., "User", "ApiClient")
- `exp`: Expiration timestamp
- `iss`: Issuer (from config)
- `aud`: Audience (from config)

## Security Notes

- All secrets and signing keys are managed via DI and secret providers.
- Always use HTTPS in production.
- Tokens expire after the configured time.
- Change credentials in production and never store secrets in code.

## Error Responses

- 401 Unauthorized: Token missing/expired/invalid
- 403 Forbidden: Valid token, insufficient permissions
- 400 Bad Request: Invalid credentials

## Architecture

- Token generation is handled in `AuthController` (no separate service).
- Credentials and signing keys are injected via DI.
- API versioning and observability are integrated (API version is logged and tagged).

## API Key Authentication

API Key authentication provides simple header-based authentication for service-to-service communication.

**Request Header:**

```http
X-Api-Key: your-api-key-here
```

> **📖 Complete Guide:** See [API Key Adapter README](../src/Adapters/emc.camus.security.apikey/README.md) for configuration, usage examples, and security best practices.

## Choosing Authentication Method

- **JWT**: User authentication, mobile apps, SPAs, fine-grained permissions
- **API Key**: Service-to-service, webhooks, legacy systems, CLI tools
- **Both**: Endpoints can accept either authentication method using `[Authorize]` with multiple schemes
