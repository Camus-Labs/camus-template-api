# Authentication Implementation

Camus API supports two authentication mechanisms: **JWT Bearer tokens** and **API Key authentication**. Both are
implemented using ASP.NET Core 9 with credentials retrieved from a secure `ISecretProvider`.

## JWT Authentication

JWT Bearer authentication provides secure token-based authentication using RSA256 asymmetric signing.

> **📖 Complete Configuration Guide:** See [JWT Adapter README](../src/Adapters/emc.camus.security.jwt/README.md) for
detailed configuration, settings, and security best practices.

## How to Get a Token

Post credentials to `POST /api/v2/auth/authenticate` to receive a JWT token, then include it in the
`Authorization: Bearer` header on subsequent requests. See the Swagger UI at `/swagger` for the complete
request/response specification.

> **📖 Complete Usage Guide:** See [JWT Adapter README](../src/Adapters/emc.camus.security.jwt/README.md) for
token generation, endpoint protection, and testing.

## JWT Claims

JWT tokens include standard claims for user identification, roles, and token metadata.

> **📖 Complete Claims Reference:** See [JWT Adapter README](../src/Adapters/emc.camus.security.jwt/README.md) for
detailed claims documentation and usage examples.

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

- Token generation is delegated to `AuthService` via the `AuthController`.
- Credentials and signing keys are injected via DI.
- API versioning and observability are integrated (API version is logged and tagged).

## API Key Authentication

API Key authentication provides simple header-based authentication for service-to-service communication.
Include the API key in the `Api-Key` request header.

> **📖 Complete Guide:** See [API Key Adapter README](../src/Adapters/emc.camus.security.apikey/README.md) for
configuration, usage examples, and security best practices.

## Choosing Authentication Method

- **JWT**: User authentication, mobile apps, SPAs, fine-grained permissions
- **API Key**: Service-to-service, webhooks, legacy systems, CLI tools
- **Both**: Endpoints can accept either authentication method using `[Authorize]` with multiple schemes
