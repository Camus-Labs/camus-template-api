# emc.camus.documentation.swagger

Swagger/OpenAPI documentation adapter for Camus applications.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides comprehensive API documentation using Swagger/OpenAPI 3.0, with support for authentication, API versioning, and interactive testing through Swagger UI.

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

```csharp
using emc.camus.documentation.swagger;

// Add Swagger documentation
builder.AddSwaggerDocumentation();

var app = builder.Build();

// Enable Swagger middleware
app.UseSwaggerDocumentation();

app.Run();
```

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

Enable XML documentation in your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Document your endpoints:

```csharp
/// <summary>
/// Retrieves a product by its ID
/// </summary>
/// <param name="id">The product identifier</param>
/// <returns>The product details</returns>
/// <response code="200">Product found successfully</response>
/// <response code="404">Product not found</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ProductDto>> GetProduct(int id)
{
    var product = await _productService.GetByIdAsync(id);
    
    if (product == null)
        return NotFound();
    
    return Ok(product);
}
```

### Swagger Annotations

Use attributes for enhanced documentation:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    /// <summary>
    /// Creates a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
```

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

The adapter automatically generates documentation for each API version:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsV1Controller : ControllerBase
{
    // Version 1 endpoints
}

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsV2Controller : ControllerBase
{
    // Version 2 endpoints with breaking changes
}
```

**Swagger UI Dropdown:**

- Select version from dropdown in top-right
- Each version shows only its endpoints
- Separate OpenAPI documents per version

---

## 🎨 Customization

### Custom Swagger Options

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Camus API",
        Version = "v1",
        Description = "API with hexagonal architecture",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
    
    // Add custom headers
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
});
```

### Request/Response Examples

```csharp
public class CreateProductRequestExample : IExamplesProvider<CreateProductRequest>
{
    public CreateProductRequest GetExamples()
    {
        return new CreateProductRequest
        {
            Name = "Laptop",
            Price = 999.99m,
            Description = "High-performance laptop"
        };
    }
}

// Register in Swagger
options.ExampleFilters();
```

---

## 🏭 Production Configuration

### Disable Swagger in Production (Optional)

```csharp
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwaggerDocumentation();
}
```

### Secure Swagger UI

```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Camus API v1");
    
    // Require authentication
    options.ConfigObject.AdditionalItems["onComplete"] = 
        "() => { ui.preauthorizeApiKey('ApiKey', 'your-key-here'); }";
});
```

---

## ⚠️ Limitations & Constraints

### Development-Only by Default

- **Disabled in Production**: `Enabled: false` by default for security
- **Environment Check**: `UseSwaggerDocumentation()` only activates in Development environment
- **Explicit Override Required**: Must set `Enabled: true` AND configure for non-development use

```csharp
// Built-in environment check in middleware
if (!settings.Enabled || !app.Environment.IsDevelopment())
{
    return app; // Swagger disabled
}
```

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

```csharp
// Example: Restrict to authenticated users
app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/swagger"),
    appBuilder =>
    {
        appBuilder.Use(async (context, next) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = 401;
                return;
            }
            await next();
        });
        appBuilder.UseSwagger();
        appBuilder.UseSwaggerUI();
    });
```

### Configuration Validation

Configuration is validated at startup with fail-fast behavior:

- At least one version required when `Enabled: true`
- SecuritySchemes must be "Bearer" or "ApiKey" (case-insensitive)
- Validation skipped when `Enabled: false`

---

## 🔗 OpenAPI Specification Export

Generate OpenAPI spec for client generation:

```bash
# Export OpenAPI JSON
curl http://localhost:5000/swagger/v1/swagger.json > openapi.json

# Generate TypeScript client
npx @openapitools/openapi-generator-cli generate \
  -i openapi.json \
  -g typescript-axios \
  -o ./clients/typescript

# Generate C# client
dotnet tool install --global Microsoft.dotnet-openapi
dotnet openapi add url http://localhost:5000/swagger/v1/swagger.json
```

---

## 🧪 Testing with Swagger UI

1. **Explore Endpoints**: Browse available operations organized by controller
2. **Try It Out**: Click "Try it out" button on any endpoint
3. **Enter Parameters**: Fill in path parameters, query strings, request body
4. **Execute**: Click "Execute" to send real request
5. **View Response**: See status code, headers, and response body

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
