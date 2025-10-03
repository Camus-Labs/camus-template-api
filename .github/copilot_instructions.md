# Copilot Instructions - CamusTemplate API

This file provides GitHub Copilot with context and coding standards for the CamusTemplate API project.

## Project Overview

**CamusTemplate API** is a modern, production-ready **.NET 9.0 REST API template** designed with **Hexagonal Architecture (Ports & Adapters)**. It provides a robust foundation for scalable, maintainable, and observable APIs with a strong focus on security, clean code, and cloud-native best practices.

## Architecture & Design Patterns

### Hexagonal Architecture (Ports & Adapters)
- **Domain** layer contains business logic and domain models (no external dependencies)
- **Application** layer contains use cases and application services
- **Adapters** layer contains infrastructure implementations (databases, external APIs)
- **API** layer contains controllers and web-specific configurations

### Project Structure
```
src/
├── CamusApp.sln
├── Api/
│   └── gto.myapp.api/                # Controllers, Program.cs, Middleware
├── Application/
│   └── gto.application/              # Use cases, Application services
├── Domain/
│   └── gto.domain/                   # Domain models, Business logic
├── Adapters/
│   └── gto.datapersistance.postgresql/ # Data access, External integrations
└── Test/
    ├── Api.Test/
    ├── Application.Test/
    ├── Domain.Test/
    └── Adapter.postgresql.Test/
```

## Coding Standards

### General Guidelines
- Follow **SOLID principles**
- Use **Clean Architecture** patterns
- Implement **Domain-Driven Design (DDD)** concepts
- Prefer **composition over inheritance**
- Write **self-documenting code** with meaningful names
- Use **nullable reference types** (enabled by default)
- Follow **Microsoft's C# coding conventions**

### Naming Conventions
- **Classes**: PascalCase (`UserService`, `AuthController`)
- **Interfaces**: PascalCase with 'I' prefix (`IUserRepository`, `IAuthService`)
- **Methods**: PascalCase (`GetUserById`, `AuthenticateUser`)
- **Properties**: PascalCase (`UserId`, `FirstName`)
- **Fields**: camelCase with underscore prefix (`_userRepository`, `_logger`)
- **Parameters**: camelCase (`userId`, `authToken`)
- **Local variables**: camelCase (`userModel`, `authResult`)
- **Constants**: PascalCase (`MaxRetryAttempts`, `DefaultTimeout`)

### File and Folder Organization
- **One class per file** (except for small, related classes)
- **File names match class names** (`UserService.cs`, `IUserRepository.cs`)
- **Folder structure reflects namespaces**
- **Group related files in appropriate folders**

## Technology Stack

### Core Technologies
- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Dapper** - Micro ORM for data access
- **PostgreSQL** - Primary database
- **Serilog** - Structured logging
- **OpenTelemetry** - Observability and tracing

### Authentication & Security
- **JWT Bearer Authentication** with RSA256
- **API Key Authentication** via `X-Api-Key` header
- **CORS** and **Rate Limiting**
- **Security Headers** (HSTS, CSP, etc.)

### Documentation & Testing
- **Swagger/OpenAPI** for API documentation
- **XML Documentation** for public APIs
- **xUnit** for unit testing
- **Moq** for mocking dependencies

## Code Patterns & Practices

### Repository Pattern
```csharp
// Interface in Domain or Application
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

// Implementation in Adapters
public class PostgreSqlUserRepository : IUserRepository
{
    // Implementation using Dapper
}
```

### Use Case Pattern
```csharp
public interface IGetUserUseCase
{
    Task<UserDto> ExecuteAsync(int userId);
}

public class GetUserUseCase : IGetUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserUseCase> _logger;

    public GetUserUseCase(IUserRepository userRepository, ILogger<GetUserUseCase> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> ExecuteAsync(int userId)
    {
        // Implementation
    }
}
```

### Controller Pattern
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UserController : ControllerBase
{
    private readonly IGetUserUseCase _getUserUseCase;

    public UserController(IGetUserUseCase getUserUseCase)
    {
        _getUserUseCase = getUserUseCase;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        // Implementation
    }
}
```

### Error Handling
- Use **global exception handling middleware**
- Return **consistent error responses**
- Log **exceptions with context**
- Use **custom exceptions** for domain-specific errors

### Logging Standards
```csharp
// Use structured logging with Serilog
_logger.LogInformation("User {UserId} retrieved successfully", userId);
_logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
```

### Dependency Injection
```csharp
// Register services in Program.cs
builder.Services.AddScoped<IUserRepository, PostgreSqlUserRepository>();
builder.Services.AddScoped<IGetUserUseCase, GetUserUseCase>();
```

## Test Code Generation Guidelines

### Unit Test Structure
- **Test business logic** in Domain and Application layers
- **Mock external dependencies** using Moq
- **Use descriptive test names** that explain the scenario
- **Follow AAA pattern** (Arrange, Act, Assert)

### Test Naming Convention
```csharp
[Fact]
public async Task GetUserById_WhenUserExists_ReturnsUserDto()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    var useCase = new GetUserUseCase(mockRepository.Object, Mock.Of<ILogger<GetUserUseCase>>());
    
    // Act
    var result = await useCase.ExecuteAsync(1);
    
    // Assert
    Assert.NotNull(result);
}
```

## Configuration Standards

### appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=camusdb;Username=user;Password=password"
  },
  "JwtSettings": {
    "RsaPrivateKeyPem": "certificate.pem",
    "Issuer": "https://api.camus.com",
    "Audience": "https://api.camus.com"
  },
  "ApiKey": "your-secure-api-key",
  "OpenTelemetry": {
    "Tracing": {
      "Exporter": "Console"
    },
    "Metrics": {
      "MetricsExporter": "Prometheus"
    }
  }
}
```

### Environment Variables
- Use **environment variables** for sensitive data in production
- Prefix with **application name** (`CAMUS_DB_CONNECTION`, `CAMUS_JWT_KEY`)

## Security Best Practices for Code Generation

### Authentication & Authorization
- **Validate JWT tokens** properly
- **Implement API key validation**
- **Validate input** at API boundaries
- **Sanitize error messages** to prevent information leakage

### Data Protection
- **Never log sensitive data** (passwords, tokens, personal info)
- **Use parameterized queries** to prevent SQL injection
- **Validate and sanitize** all user inputs

## Performance Guidelines for Code Generation

### Database Access
- **Use async/await** for all database operations
- **Use Dapper** for high-performance queries
- **Avoid N+1 queries**

### API Design
- **Use pagination** for large result sets
- **Use appropriate HTTP status codes**
- **Design RESTful endpoints**

## Documentation Requirements

### XML Documentation
```csharp
/// <summary>
/// Retrieves a user by their unique identifier.
/// </summary>
/// <param name="userId">The unique identifier of the user.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the user data.</returns>
/// <exception cref="UserNotFoundException">Thrown when the user is not found.</exception>
public async Task<UserDto> GetUserByIdAsync(int userId)
{
    // Implementation
}
```

### API Documentation
- **Use Swagger annotations** for API documentation
- **Provide example requests/responses**
- **Document authentication requirements**

---

*This document should be updated as the project evolves and new patterns are established.*