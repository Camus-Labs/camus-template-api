# emc.camus.documentation.swagger

Swagger/OpenAPI documentation adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides comprehensive API documentation using Swagger/OpenAPI 3.0, with support for authentication, API
versioning, and interactive testing through Swagger UI.

---

## ✨ Features

- 📚 **OpenAPI 3.0 Specification** - Industry-standard API documentation
- 🎨 **Interactive UI** - Swagger UI for testing endpoints
- 🔐 **Authentication Support** - JWT Bearer and API Key documentation
- 📌 **API Versioning** - Multi-version documentation support
- 📝 **XML Comments** - Automatic documentation from code comments
- 🎯 **Request/Response Examples** - Clear API contract definition

---

## 🚀 Usage

### 1. Register in Program.cs

Call `builder.AddSwaggerDocumentation()` to register Swagger services, then call `app.UseSwaggerDocumentation()` to
enable the middleware. See `SwaggerSetupExtensions` in this adapter for the full registration API.

### 2. Configure Settings

In `appsettings.json`:

```json
{
  "SwaggerSettings": {
    "Enabled": true,
    "Versions": [
      {
        "Version": "v1",
        "Title": "Camus API v1.0",
        "Description": "Production-ready .NET API with Hexagonal Architecture"
      },
      {
        "Version": "v2",
        "Title": "Camus API v2.0",
        "Description": "Enhanced API with additional features"
      }
    ],
    "SecuritySchemes": [ "Bearer", "ApiKey" ],
    "IncludeXmlComments": true,
    "EnableAnnotations": true,
    "EnableExampleFilters": true,
    "RedirectRootToSwagger": true
  }
}
```

**Configuration Properties:**

- **Enabled** - Enable/disable Swagger (default: `false`, recommended for development only)
- **Versions** - Array of API versions to document (required when enabled)
  - **Version** - Version identifier (e.g., "v1", "v2")
  - **Title** - Display title for this version
  - **Description** - Version description (optional)
- **SecuritySchemes** - Authentication schemes to document (`"Bearer"`, `"ApiKey"`)
- **IncludeXmlComments** - Include XML documentation comments (default: `false`)
- **EnableAnnotations** - Enable Swagger annotations (default: `false`)
- **EnableExampleFilters** - Enable request/response examples (default: `false`)
- **RedirectRootToSwagger** - Redirect `/` to Swagger UI (default: `true`)

### 3. Access Swagger UI

Once running, navigate to:

- **Swagger UI**: `http://localhost:5000/swagger`
- **OpenAPI JSON**: `http://localhost:5000/swagger/v1/swagger.json`

---

## 📝 Documenting Your API

### XML Documentation Comments

XML documentation generation is enabled centrally in `src/Directory.Build.props` — no per-project configuration is needed.

Document endpoints using standard XML summary, param, returns, and response tags, along with `[ProducesResponseType]`
attributes. See controller source files in `src/Api/emc.camus.api/Controllers/` for examples.

### Swagger Annotations

Use `[ApiVersion]`, `[Produces]`, and `[ProducesResponseType]` attributes for enhanced documentation. See controller
source files for annotation patterns.

---

## 🔐 Authentication in Swagger UI

### JWT Bearer Token

1. Click **"Authorize"** button in Swagger UI
2. Get token from `/api/v2/auth/token` endpoint
3. Enter: `Bearer <your-token>`
4. Click **"Authorize"**
5. Test protected endpoints

### API Key

1. Click **"Authorize"** button
2. Enter your API key in the `X-Api-Key` field
3. Click **"Authorize"**
4. Test protected endpoints

---

## 📌 API Versioning

The adapter automatically generates documentation for each API version. Define versioned controllers using
`[ApiVersion]` and version-prefixed routes — Swagger UI presents a dropdown to switch between versions, each with its
own OpenAPI document.

See controller source files in `src/Api/emc.camus.api/Controllers/` for versioning patterns.

**Swagger UI Dropdown:**

- Select version from dropdown in top-right
- Each version shows only its endpoints
- Separate OpenAPI documents per version

---

## 🎨 Customization

### Custom Swagger Options

Customize the OpenAPI document metadata (title, description, contact, license) and add additional security definitions
by configuring `SwaggerGenOptions` in `SwaggerSetupExtensions.cs`.

### Request/Response Examples

Implement `IExamplesProvider<T>` for request/response example generation and register with `options.ExampleFilters()`.
See `SwaggerSetupExtensions.cs` for the existing configuration.

---

## 🏭 Production Configuration

### Disable Swagger in Production (Optional)

The `UseSwaggerDocumentation()` extension checks the `Enabled` setting and the hosting environment. Swagger is only
active when `Enabled: true` and the environment is Development. See `SwaggerSetupExtensions.cs` for the built-in
environment check.

---

## ⚠️ Limitations & Constraints

### Development-Only by Default

- **Disabled in Production**: `Enabled: false` by default for security
- **Environment Check**: `UseSwaggerDocumentation()` only activates in Development environment
- **Explicit Override Required**: Must set `Enabled: true` AND configure for non-development use

The middleware checks both the `Enabled` flag and the hosting environment before activating.

### Security Considerations

**⚠️ Exposing Swagger in Production:**

Swagger UI exposes:

- Complete API structure and endpoints
- Request/response schemas
- Authentication mechanisms
- Internal error details

**If production exposure is required:**

1. **Add Authentication**: Protect Swagger UI endpoint
2. **Whitelist IPs**: Restrict access to internal networks
3. **Remove Sensitive Data**: Sanitize examples and descriptions
4. **Disable Try-It-Out**: Prevent direct API calls from UI

See `SwaggerSetupExtensions.cs` for middleware configuration options.

### Configuration Validation

Configuration is validated at startup with fail-fast behavior:

- At least one version required when `Enabled: true`
- SecuritySchemes must be "Bearer" or "ApiKey" (case-insensitive)
- Validation skipped when `Enabled: false`

---

## 🧪 Testing with Swagger UI

1. **Explore Endpoints**: Browse available operations organized by controller
2. **Try It Out**: Click "Try it out" button on any endpoint
3. **Enter Parameters**: Fill in path parameters, query strings, request body
4. **Execute**: Click "Execute" to send real request
5. **View Response**: See status code, headers, and response body

---

## 🔗 Integration

The adapter registers Swagger services and middleware via two extension methods in `SwaggerSetupExtensions.cs`:

1. **`builder.AddSwaggerDocumentation()`** — Reads `SwaggerSettings` from configuration, validates versions and security
  schemes, and registers SwaggerGen with OpenAPI documents, security definitions, XML comments, and annotation support.
2. **`app.UseSwaggerDocumentation()`** — Activates Swagger and SwaggerUI middleware only when `Enabled: true` and the
  environment is Development. Optionally redirects the root URL to Swagger UI.

Call these in `Program.cs` after other service registrations and before `app.Run()`.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| Swagger UI not loading | `SwaggerSettings:Enabled` is `false` or environment is not Development |
| No endpoints visible | Controllers missing `[ApiVersion]` attribute or route template mismatch |
| XML comments not appearing | `IncludeXmlComments` is `false` or XML doc file not generated (check `Directory.Build.props`) |
| Security "Authorize" button missing | `SecuritySchemes` array is empty or contains invalid values |
| 404 on `/swagger` | `UseSwaggerDocumentation()` not called or called after `app.Run()` |

---

## 🔗 Related Documentation

- **[Architecture Guide](../../../../docs/architecture.md)** - API layer architecture
- **[Authentication Guide](../../../../docs/authentication.md)** - Authentication configuration
- **[OpenAPI Specification](https://swagger.io/specification/)** - Official OpenAPI docs
- **[Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)** - Swagger for ASP.NET Core

---

## 📦 Dependencies

- `Swashbuckle.AspNetCore` - Swagger generation and UI
- `Swashbuckle.AspNetCore.Annotations` - Enhanced documentation attributes
- `Swashbuckle.AspNetCore.Filters` - Request/response examples
- Microsoft.AspNetCore.Mvc.Versioning
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection

---

## 💡 Best Practices

- ✅ Use XML comments for all public APIs
- ✅ Provide request/response examples
- ✅ Document all possible status codes
- ✅ Use meaningful operation IDs
- ✅ Group endpoints logically with tags
- ✅ Keep descriptions concise but informative
- ✅ Version your API properly
- ❌ Don't expose internal implementation details
- ❌ Don't document deprecated endpoints without marking them
