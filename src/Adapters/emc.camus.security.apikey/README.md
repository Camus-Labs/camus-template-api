# emc.camus.security.apikey

API Key authentication adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Authentication Guide](../../../../docs/authentication.md)

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

```csharp
using emc.camus.security.apikey;

// Register secret provider first
builder.AddDaprSecrets();

// Add API Key authentication
builder.AddApiKeyAuthentication(serviceName);

var app = builder.Build();

// Enable authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

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

```csharp
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    // Require API Key authentication
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]
    [HttpGet]
    public IActionResult GetData()
    {
        return Ok(new { message = "Authenticated with API Key" });
    }
    
    // Support both JWT and API Key
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{AuthenticationSchemes.ApiKey}")]
    [HttpGet("flexible")]
    public IActionResult GetFlexibleData()
    {
        return Ok(new { message = "Authenticated with JWT or API Key" });
    }
}
```

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

## 🧪 Example Requests

### Using curl

```bash
curl -H "X-Api-Key: your-api-key-12345" \
     http://localhost:5000/api/data
```

### Using Postman

```bash
GET http://localhost:5000/api/data
Headers:
  X-Api-Key: your-api-key-12345
```

### Using C# HttpClient

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Api-Key", "your-api-key-12345");

var response = await client.GetAsync("http://localhost:5000/api/data");
```

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

Support both authentication methods:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using emc.camus.application.Auth;

// Accept either JWT or API Key
[Authorize(AuthenticationSchemes = 
    $"{JwtBearerDefaults.AuthenticationScheme},{AuthenticationSchemes.ApiKey}")]
public class FlexibleController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var authType = User.Identity?.AuthenticationType;
        return Ok(new { authenticated = true, type = authType });
    }
}
```

---

## 🧪 Testing

### Unit Tests

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Moq;
using Xunit;
using emc.camus.application.Secrets;
using emc.camus.security.apikey.Handlers;
using emc.camus.security.apikey.Configurations;
using emc.camus.security.apikey.Metrics;

var mockSecretProvider = new Mock<ISecretProvider>();
mockSecretProvider
    .Setup(x => x.GetSecret("XApiKey"))
    .Returns("test-api-key");

var mockMetrics = new Mock<ApiKeyMetrics>("test-service");
var mockOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
mockOptions
    .Setup(x => x.Get(It.IsAny<string>()))
    .Returns(new AuthenticationSchemeOptions());
var mockLogger = new Mock<ILoggerFactory>();
var encoder = UrlEncoder.Default;
var settings = new ApiKeySettings { SecretKeyName = "XApiKey" };

var handler = new ApiKeyAuthenticationHandler(
    mockOptions.Object,
    mockLogger.Object,
    encoder,
    mockSecretProvider.Object,
    settings,
    mockMetrics.Object
);

// Test authentication logic
```

### Integration Tests

```csharp
[Fact]
public async Task GetData_WithValidApiKey_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
    
    // Act
    var response = await client.GetAsync("/api/data");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task GetData_WithInvalidApiKey_ReturnsUnauthorized()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
    
    // Act
    var response = await client.GetAsync("/api/data");
    
    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

---

## 🔗 Related Documentation

- **[JWT Authentication Adapter](../emc.camus.security.jwt/README.md)** - Token-based authentication
- **[Authentication Guide](../../../../docs/authentication.md)** - Complete authentication overview
- **[Secrets Adapter](../emc.camus.secrets.dapr/README.md)** - Secret management
- **[Architecture Guide](../../../../docs/architecture.md)** - Security architecture

---

## 📊 Observability

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
