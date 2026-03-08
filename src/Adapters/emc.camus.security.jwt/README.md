# emc.camus.security.jwt

JWT (JSON Web Token) authentication adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Authentication Guide](../../../../docs/authentication.md)

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

```csharp
using emc.camus.security.jwt;

// Register secret provider first
builder.AddDaprSecrets();

// Add JWT authentication
builder.AddJwtAuthentication();

var app = builder.Build();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

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

> **📖 Secret Management:** See [Dapr Secrets Adapter](../emc.camus.secrets.dapr/README.md) for secret provider configuration.

---

## 🔐 Token Generation

### Using IJwtTokenGenerator

The adapter provides `IJwtTokenGenerator` interface for token generation. Inject it into your authentication controller:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISecretProvider _secretProvider;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        ISecretProvider secretProvider,
        IJwtTokenGenerator tokenGenerator,
        ILogger<AuthController> logger)
    {
        _secretProvider = secretProvider;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }
    
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] Credentials request)
    {
        // Validate credentials against secrets
        var accessKey = _secretProvider.GetSecret("AccessKey");
        var accessSecret = _secretProvider.GetSecret("AccessSecret");
        
        if (request.AccessKey != accessKey || request.AccessSecret != accessSecret)
        {
            var exception = new UnauthorizedAccessException("Invalid credentials");
            exception.Data[ErrorCodes.ErrorCodeKey] = ErrorCodes.InvalidCredentials;
            throw exception;
        }
        
        // Generate token with custom claims
        var roleClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "ApiClient")
        };
        
        var command = new GenerateTokenCommand(request.AccessKey, roleClaims);
        var result = _tokenGenerator.GenerateToken(command);
        
        return Ok(new AuthenticateUserResponse
        {
            Token = result.Token,
            ExpiresOn = result.ExpiresOn
        });
    }
}
```

**Key Points:**

- ✅ Inject `IJwtTokenGenerator` via constructor
- ✅ Token generation is handled by the adapter
- ✅ Add custom claims as needed (roles, permissions, etc.)
- ✅ Returns `GenerateTokenResult` with token and expiration

---

## 🎯 Protecting Endpoints

### Require JWT Authentication

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    // Require JWT authentication
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet]
    public IActionResult GetProducts()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { message = $"Authenticated user: {userId}" });
    }
    
    // Require specific role
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public IActionResult DeleteProduct(int id)
    {
        // Only users with "Admin" role can access
        return NoContent();
    }
    
    // Custom policy
    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost]
    public IActionResult CreateProduct(ProductDto product)
    {
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
```

### Support Multiple Authentication Schemes

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

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
var username = User.FindFirst("unique_name")?.Value;
```

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
- **Misconfiguration**: Sustained `invalid_issuer` or `invalid_audience` errors indicate configuration mismatch
- **User Experience**: High rate of `token_expired` may indicate expiration window is too short
- **Security Monitoring**: Track which endpoints are being targeted by unauthorized access attempts

**Example Queries:**

```promql
# Total authentication failures in the last hour
sum(increase(jwt_authentication_failures_total[1h]))

# Failure rate by reason
sum by (failure_reason) (rate(jwt_authentication_failures_total[5m]))

# Endpoints under attack (high invalid_signature rate)
topk(5, sum by (endpoint) (rate(jwt_authentication_failures_total{failure_reason="invalid_signature"}[5m])))
```

### Error Handling

Authentication failures are handled via exceptions with error codes in `exception.Data[ErrorCodes.ErrorCodeKey]`. The
global exception handler logs errors and returns RFC 7807 Problem Details responses.

---

## 🔒 Error Codes

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

```typescript
// Example: TypeScript client handling JWT errors
if (response.status === 401) {
  const error = response.body.error;
  
  if (error === 'token_expired') {
    // Refresh token or redirect to login
    await refreshToken();
  } else if (error === 'invalid_signature') {
    // Security issue, clear token and re-authenticate
    clearToken();
    redirectToLogin();
  }
}
```

> **📖 Error Codes Reference:** See [ErrorCodes.cs](../../../Application/emc.camus.application/Generic/ErrorCodes.cs)
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

Generate secure RSA keys:

```bash
# Generate private key
openssl genrsa -out private.pem 2048

# Extract public key (for verification)
openssl rsa -in private.pem -pubout -out public.pem

# View key contents
cat private.pem
```

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

```csharp
var mockSecretProvider = new Mock<ISecretProvider>();
mockSecretProvider
    .Setup(x => x.GetSecretAsync("RsaPrivateKeyPem"))
    .ReturnsAsync("-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----");

// Test token generation logic
```

### Integration Tests

```csharp
[Fact]
public async Task GetProducts_WithValidToken_ReturnsOk()
{
    // Arrange
    var token = await GetTestTokenAsync();
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## 🔗 Related Documentation

- **[API Key Authentication Adapter](../emc.camus.security.apikey/README.md)** - Alternative authentication method
- **[Authentication Guide](../../../../docs/authentication.md)** - Complete authentication overview
- **[Secrets Adapter](../emc.camus.secrets.dapr/README.md)** - Secret management
- **[Architecture Guide](../../../../docs/architecture.md)** - Security architecture
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

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = rsaSecurityKey,
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };
    });
```

### Custom Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("email_verified", "true"));
});
```

```csharp
builder.AddCamusAuthorization();

// Add custom policies after
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireApiClient", policy =>
        policy.RequireClaim(ClaimTypes.Role, "ApiClient"));
});
```

## Design Principles

✅ **Separation of Concerns** - Authentication and authorization split  
✅ **Loose Coupling** - Depends only on abstractions  
✅ **Reusability** - Can be used across multiple APIs  
✅ **Testability** - Easy to mock `ISecretProvider` for tests  
✅ **Maintainability** - All security logic in one place  
✅ **Extensibility** - Ready for complex authorization scenarios  
✅ **Maintainability** - All authentication logic in one place
