# emc.camus.security.jwt

JWT (JSON Web Token) authentication adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Authentication Guide](../../../../docs/authentication.md)

---

## 📋 Overview

This adapter provides JWT Bearer authentication with RSA256 signing, enabling secure token-based authentication for your API. It integrates with the Application layer's `ISecretProvider` for secure key management.

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

### Authentication Controller

Tokens are typically generated in your authentication controller:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISecretProvider _secretProvider;
    private readonly IConfiguration _configuration;
    
    [HttpPost("token")]
    public async Task<ActionResult<JwtTokenResponse>> GetToken(JwtTokenRequest request)
    {
        // Validate credentials (implement your logic)
        if (!await ValidateCredentialsAsync(request))
            return Unauthorized();
        
        // Generate token
        var token = GenerateJwtToken(request.AccessKey);
        
        return Ok(new JwtTokenResponse
        {
            Token = token,
            ExpiresOn = DateTime.UtcNow.AddMinutes(60)
        });
    }
    
    private string GenerateJwtToken(string userId)
    {
        var settings = _configuration.GetSection(JwtSettings.ConfigurationSectionName).Get<JwtSettings>();
        settings.Validate();
        builder.Services.AddSingleton(settings);

        var rsaKey = await _secretProvider.GetSecretAsync("RsaPrivateKeyPem");
        
        var rsa = RSA.Create();
        rsa.ImportFromPem(rsaKey);
        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa), 
            SecurityAlgorithms.RsaSha256
        );
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", "User")
        };
        
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

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
