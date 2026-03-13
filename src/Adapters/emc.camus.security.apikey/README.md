# emc.camus.security.apikey

API Key authentication adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Authentication Guide](../../../docs/authentication.md)

---

## 📋 Overview

This adapter implements API Key authentication using the `X-Api-Key` header, providing a simple authentication mechanism for service-to-service communication or legacy client support.

---

## ✨ Features

- 🔑 **Header-Based Authentication** - Uses `X-Api-Key` header
- 🔒 **Secure Key Storage** - Keys retrieved from secret provider
- 🎯 **ASP.NET Core Integration** - Standard authentication middleware
- 🔄 **Interface-Based** - Depends on `ISecretProvider` from Application layer
- ⚙️ **Configurable** - Settings via `appsettings.json`

---

## 🚀 Usage

### 1. Register in Program.cs

Register the secret provider first (`builder.AddDaprSecrets()`), then call `builder.AddApiKeyAuthentication(serviceName)` to add API Key authentication. Enable the standard ASP.NET Core authentication/authorization middleware.

See `ApiKeySetupExtensions` in this adapter for the full registration API.

### 2. Configure Settings (Optional)

In `appsettings.json`:

```json
{
  "ApiKeySettings": {
    "SecretKeyName": "XApiKey"  // Optional - defaults to "XApiKey"
  }
}
```

> **Note:** The `SecretKeyName` setting is optional and defaults to `"XApiKey"`. Only configure this if you need to use a different secret name.
> **Note:** The API Key header name is fixed as `X-Api-Key` and cannot be configured.

### 3. Protect Endpoints

Apply `[Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]` to controllers or actions that require API Key authentication. To accept both JWT and API Key, combine scheme names in the `AuthenticationSchemes` parameter.

See controller source files in `src/Api/emc.camus.api/Controllers/` for examples.

---

## 🔐 How It Works

### Request Flow

```text
Client Request
    ↓
HTTP Header: X-Api-Key: your-api-key-here
    ↓
ApiKeyAuthenticationHandler
    ├─→ Extracts key from header
    ├─→ Retrieves expected key from ISecretProvider
    ├─→ Compares keys (constant-time comparison)
    └─→ Creates ClaimsPrincipal if valid
    ↓
[Authorize] Attribute Validation
    ↓
Controller Action
```

### Security Features

- **Constant-Time Comparison** - Prevents timing attacks
- **Secret Provider Integration** - Keys never in code or config files
- **Claims-Based Identity** - Standard ASP.NET Core authentication
- **Flexible Authorization** - Combine with policy-based authorization

---

## ⚙️ Configuration

### Secret Provider Setup

The adapter retrieves the expected API key from `ISecretProvider`:

**Development** (`src/Infrastructure/dapr/secrets.json`):

```json
{
  "XApiKey": "dev-api-key-12345"
}
```

**Production** (Azure Key Vault, AWS Secrets Manager, etc.):

```bash
# Azure CLI
az keyvault secret set --vault-name your-vault --name XApiKey --value "prod-key-xyz"
```

> **📖 Secrets Management:** See [Dapr Secrets Adapter](../emc.camus.secrets.dapr/README.md) for secret provider configuration.

---

## 🏗️ Architecture

### Dependency Inversion

```text
┌──────────────────────────────────────┐
│      Application Layer               │
│        ISecretProvider               │
└───────────────┬──────────────────────┘
                │ depends on
┌───────────────▼──────────────────────┐
│       Adapter Layer                  │
│  ApiKeyAuthenticationHandler         │
│  (uses ISecretProvider)              │
└───────────────┬──────────────────────┘
                │
┌───────────────▼──────────────────────┐
│   ASP.NET Core Authentication        │
│         Middleware                   │
└──────────────────────────────────────┘
```

---

## 🎯 Use Cases

**✅ Good For:**

- Service-to-service communication
- Legacy client integration
- Simple authentication requirements
- Webhook callbacks
- CLI tools

**⚠️ Not Ideal For:**

- User authentication (use JWT instead)
- Browser-based applications (use JWT)
- Fine-grained authorization (use JWT with claims)

---

## 🔗 Combined with JWT

To accept either JWT or API Key on an endpoint, list both scheme names in the `[Authorize]` attribute’s `AuthenticationSchemes` parameter. See controller source files for the combined-scheme pattern.

---

## 🧪 Testing

### Unit Tests

Mock `ISecretProvider` to return a known API key, then instantiate `ApiKeyAuthenticationHandler` with the mock. See `src/Test/` for existing test examples.

### Integration Tests

Use `WebApplicationFactory` to create a test client, set the `X-Api-Key` header, and assert on the response status code. See test projects in `src/Test/` for integration test patterns.

---

## 🔗 Related Documentation

- **[JWT Authentication Adapter](../emc.camus.security.jwt/README.md)** - Token-based authentication
- **[Authentication Guide](../../../docs/authentication.md)** - Complete authentication overview
- **[Secrets Adapter](../emc.camus.secrets.dapr/README.md)** - Secret management
- **[Architecture Guide](../../../docs/architecture.md)** - Security architecture

---

## Integration

The adapter registers via the extension method in `ApiKeySetupExtensions.cs`:

- **`builder.AddApiKeyAuthentication()`** — Reads `ApiKeySettings` from configuration, resolves the expected API key from the secret provider, and registers the API-key authentication handler in the DI container.

Apply the `[Authorize(AuthenticationSchemes = "ApiKey")]` attribute to controllers or actions that require API-key authentication.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| 401 on every request | Secret provider not returning the expected key, or `X-Api-Key` header missing |
| `SecretKeyName` not found | Secret name in `ApiKeySettings` doesn't match a secret in the store |
| Header ignored | Using wrong header name — must be `X-Api-Key` (case-sensitive) |
| Works locally, fails in production | Secret store not configured for production environment |
| High `apikey_authentication_failures_total` | Possible brute-force attempt or client misconfiguration |

---

## Observability

The adapter exports the following metrics via OpenTelemetry:

### Metrics

**`apikey_authentication_failures_total`**

- **Type:** Counter
- **Description:** Total number of API Key authentication failures
- **Unit:** requests
- **Labels:**
  - `failure_reason`: Error code (`authentication_required` | `invalid_credentials`)
  - `endpoint`: The request path that was attempted

**Example Queries:**

```promql
# Rate of API Key authentication failures over 5 minutes
rate(apikey_authentication_failures_total[5m])

# Total failures by reason
sum by (failure_reason) (apikey_authentication_failures_total)

# Failures by endpoint
sum by (endpoint) (apikey_authentication_failures_total)
```

**Alerting Example:**

```yaml
# Alert on high API Key authentication failure rate
- alert: HighApiKeyAuthFailureRate
  expr: rate(apikey_authentication_failures_total[5m]) > 10
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "High API Key authentication failure rate detected"
    description: "More than 10 API Key auth failures per second over 5 minutes"
```

---

## ⚠️ Security Best Practices

- ✅ Use long, random API keys (min 32 characters)
- ✅ Rotate keys regularly
- ✅ Use different keys per environment
- ✅ Use HTTPS in production (never send keys over HTTP)
- ✅ Implement rate limiting
- ✅ Log authentication failures
- ❌ Never commit keys to version control
- ❌ Never send keys in query parameters or URL
- ❌ Never log API keys

---

## 📦 Dependencies

- `emc.camus.application` - Application interfaces (`ISecretProvider`)
- Microsoft.AspNetCore.Authentication
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
