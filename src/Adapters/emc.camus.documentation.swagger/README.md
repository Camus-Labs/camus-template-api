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
    "Title": "Camus API",
    "Description": "Production-ready .NET API with Hexagonal Architecture",
    "Version": "v1",
    "EnableXmlComments": true,
    "EnableAnnotations": true
  }
}
```

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
