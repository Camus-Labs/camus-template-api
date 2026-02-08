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
builder.AddApiKeyAuthentication();

var app = builder.Build();

// Enable authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "ApiKeySettings": {
    "HeaderName": "X-Api-Key",
    "SecretName": "XApiKey"
  }
}
```

### 3. Protect Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    // Require API Key authentication
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    [HttpGet]
    public IActionResult GetData()
    {
        return Ok(new { message = "Authenticated with API Key" });
    }
    
    // Support both JWT and API Key
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthenticationHandler.SchemeName}")]
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

### Custom Header Name

To use a different header name:

```json
{
  "ApiKeySettings": {
    "HeaderName": "X-Custom-Api-Key",
    "SecretName": "CustomApiKey"
  }
}
```

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
// Accept either JWT or API Key
[Authorize(AuthenticationSchemes = 
    $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthenticationHandler.SchemeName}")]
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
var mockSecretProvider = new Mock<ISecretProvider>();
mockSecretProvider
    .Setup(x => x.GetSecretAsync("XApiKey"))
    .ReturnsAsync("test-api-key");

var handler = new ApiKeyAuthenticationHandler(
    mockSecretProvider.Object,
    /* other dependencies */
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
