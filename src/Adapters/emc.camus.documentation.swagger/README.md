# emc.camus.documentation.swagger

Swagger/OpenAPI documentation adapter for Camus applications.

> Parent Documentation: [Main README](../../../README.md) | [Documentation Index](../../../docs/README.md) |
[Architecture Guide](../../../docs/architecture.md)

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

### Register in Program.cs

Call `builder.AddSwaggerDocumentation()` to register Swagger services, then call `app.UseSwaggerDocumentation()` to
enable the middleware. See `SwaggerSetupExtensions` in this adapter for the full registration API.

---

## ⚙️ Configuration

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
    "SecuritySchemes": [ "Bearer", "ApiKey" ]
  }
}
```

**Configuration Properties:**

- **Enabled** - Enable/disable Swagger (default: `false`, recommended for development only)
- **Versions** - Array of API versions to document (required when enabled)
  - **Version** - Version identifier (e.g., "v1", "v2")
  - **Title** - Display title for this version
  - **Description** - Version description
- **SecuritySchemes** - Authentication schemes to document (`"Bearer"`, `"ApiKey"`)

### Access Swagger UI

Once running, navigate to:

- **Swagger UI**: `/swagger`
- **OpenAPI JSON**: `/swagger/{version}/swagger.json`

See the [Main README](../../../README.md) for environment-specific host and port details.

---

## 📝 Documenting Your API

XML documentation generation is enabled centrally in `src/Directory.Build.props` — no per-project configuration is
needed. See controller source files in `src/Api/emc.camus.api/Controllers/` for annotation patterns.

---

## 🔐 Authentication in Swagger UI

### JWT Bearer Token

1. Click **"Authorize"** button in Swagger UI
2. Obtain a token following the [Authentication Guide](../../../docs/authentication.md)
3. Enter the token value in the dialog
4. Click **"Authorize"**
5. Test protected endpoints

### API Key

1. Click **"Authorize"** button
2. Enter your API key in the `Api-Key` field
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

Customize the OpenAPI document metadata (title, version, and description) and configure security definitions (Bearer,
ApiKey) via `SwaggerSettings` in `appsettings.json`. See `SwaggerSetupExtensions.cs` for the full registration logic.

### Request/Response Examples

The adapter discovers example providers from the API assembly at startup. See `SwaggerSetupExtensions.cs` for the
existing configuration.

---

## ⚠️ Limitations & Constraints

### Development-Only by Default

- **Disabled in Production**: `Enabled: false` by default for security
- **Environment Check**: `UseSwaggerDocumentation()` only activates when `Enabled: true` and the environment is
  Development — see `SwaggerSetupExtensions.cs` for the built-in environment check
- **No Built-In Override**: Code changes are required to serve Swagger outside Development

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

Configuration is validated at startup with fail-fast behavior — see `SwaggerSettings.cs` for the full set of
validation rules.

---

## 🧪 Testing with Swagger UI

1. **Explore Endpoints**: Browse available operations organized by controller
2. **Try It Out**: Click "Try it out" button on any endpoint
3. **Enter Parameters**: Fill in path parameters, query strings, request body
4. **Execute**: Click "Execute" to send real request
5. **View Response**: See status code, headers, and response body

---

## 🔌 Integration

The adapter registers Swagger services and middleware via two extension methods in `SwaggerSetupExtensions.cs`:

1. **`builder.AddSwaggerDocumentation()`** — Reads `SwaggerSettings` from configuration, validates versions and
  security schemes, and registers SwaggerGen with OpenAPI docs, security definitions, XML comments, and annotations.
2. **`app.UseSwaggerDocumentation()`** — Activates Swagger and SwaggerUI middleware only when `Enabled: true` and the
  environment is Development. Redirects the root URL to Swagger UI.

Call these in `Program.cs` after other service registrations and before `app.Run()`.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause | Fix |
| ------- | ------------ | --- |
| Swagger UI not loading | `Enabled` is `false` or environment is not Development | Set `SwaggerSettings:Enabled` to `true` and ensure the app runs in the Development environment |
| Empty OpenAPI document (no endpoints) | No `Versions` entries configured or controller routes do not match the version prefix | Add at least one entry to `SwaggerSettings:Versions` and verify controllers use matching `[ApiVersion]` attributes |
| "Authorize" button missing | `SecuritySchemes` array is empty or contains invalid scheme names | Add `"Bearer"` and/or `"ApiKey"` to `SwaggerSettings:SecuritySchemes` |
| Startup crash with `InvalidOperationException` | Configuration validation failed | Check the exception message — ensure all required fields (`Version`, `Title`, `Description`) are non-empty and scheme names are valid |
| Swagger UI not loading | `SwaggerSettings:Enabled` is `false` or environment is not Development | Set `Enabled: true` and run with `ASPNETCORE_ENVIRONMENT=Development` |
| No endpoints visible | Controllers missing `[ApiVersion]` attribute or route template mismatch | Add `[ApiVersion]` and verify route templates |
| XML comments not appearing | XML doc file not generated (check `Directory.Build.props`) | Confirm `<GenerateDocumentationFile>` is enabled |
| Security "Authorize" button missing | `SecuritySchemes` array is empty | Add `"Bearer"` or `"ApiKey"` to `SecuritySchemes` |
| Application fails to start with `InvalidOperationException` | `SecuritySchemes` contains an invalid value (valid: `Bearer`, `ApiKey`) | Use only `"Bearer"` or `"ApiKey"` (case-insensitive) |
| 404 on `/swagger` | `UseSwaggerDocumentation()` not called or called after `app.Run()` | Call `app.UseSwaggerDocumentation()` before `app.Run()` |

---

## 🔗 Related Documentation

- **[Architecture Guide](../../../docs/architecture.md)** - API layer architecture
- **[Authentication Guide](../../../docs/authentication.md)** - Authentication configuration
- **[OpenAPI Specification](https://swagger.io/specification/)** - Official OpenAPI docs
- **[Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)** - Swagger for ASP.NET Core

---

## 📦 Dependencies

- `Swashbuckle.AspNetCore` - Swagger generation and UI
- `Swashbuckle.AspNetCore.Annotations` - Enhanced documentation attributes
- `Swashbuckle.AspNetCore.Filters` - Request/response examples
- `Asp.Versioning.Mvc.ApiExplorer` - API versioning support
- `Microsoft.AspNetCore.OpenApi` - OpenAPI integration
