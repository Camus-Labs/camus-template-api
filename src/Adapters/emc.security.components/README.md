# emc.camus.security.components

Security adapter for Camus applications, providing authentication and authorization infrastructure.

## Overview

This adapter provides reusable security components for ASP.NET Core applications, including:

- **Authentication** - JWT Bearer and API Key authentication mechanisms
- **Authorization** - Extensible authorization policies and role-based access control

## Features

- 🔐 **JWT Bearer Authentication** - Token-based authentication with RSA signing
- 🔑 **API Key Authentication** - Header-based authentication using secret keys
- 🛡️ **Authorization Policies** - Extensible authorization configuration
- 🔄 **Dependency Injection** - Seamless integration with ASP.NET Core DI
- 🎯 **Interface-Based Design** - Depends on `ISecretProvider` from Application layer
- ⚙️ **Configurable** - Settings via `appsettings.json`

## Usage

### 1. Register Security Services

In your `Program.cs`:

```csharp
// First, register your secret provider implementation
builder.Services.AddDependencyInjections(builder.Configuration);

// Then, add Camus security (authentication + authorization)
builder.AddCamusAuthentication();
builder.AddCamusAuthorization();
```

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "https://auth.camuslabs.com/",
    "Audience": "https://app.camuslabs.com/",
    "ExpirationMinutes": 60
  }
}
```

### 3. Required Secrets

The adapter expects these secrets from your `ISecretProvider`:

- `RsaPrivateKeyPem` - RSA private key in PEM format for JWT signing
- `XApiKey` - API key for X-API-Key header authentication

### 4. Use in Controllers

```csharp
// JWT Authentication
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MyController : ControllerBase { }

// API Key Authentication
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class MyController : ControllerBase { }
```

## Architecture

This adapter follows the **Adapter Pattern** and **Dependency Inversion Principle**:

### Separation of Concerns

- **Authentication** (`AddCamusAuthentication`) - Who are you?
- **Authorization** (`AddCamusAuthorization`) - What can you do?

### Project Structure

``` text
emc.security.components/
├── SecuritySetupExtensions.cs       // Main entry point
├── Handlers/
│   ├── JwtAuthenticationHandler.cs  // JWT Bearer configuration
│   └── ApiKeyAuthenticationHandler.cs // API Key handler
└── Configurations/
    └── JwtSettings.cs                // JWT settings
```

### Dependencies

- Depends on **interfaces** (`ISecretProvider`) from the Application layer
- Does **not** depend on concrete implementations (e.g., `DaprSecretProvider`)
- Host application provides concrete implementations through DI

## Extending Authorization

Add custom authorization policies in your API:

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
