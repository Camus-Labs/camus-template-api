# emc.camus.persistence.postgresql

PostgreSQL database adapter for Camus applications using Dapper and Npgsql.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides PostgreSQL database access using Dapper micro-ORM, implementing the repository pattern for clean separation between data access and business logic. It supports both API information and user authorization data persistence.

---

## ✨ Features

- 🗄️ **PostgreSQL Integration** - Optimized for PostgreSQL with Npgsql
- ⚡ **Dapper Micro-ORM** - Lightweight, fast SQL mapping
- 🎯 **Repository Pattern** - Abstraction for data access
- 🔄 **Async/Await** - Non-blocking database operations
- ⚙️ **Connection Pooling** - Built-in connection management
- 🔐 **Secret Provider Integration** - Secure password management
- 🧪 **Testable** - Interface-based design

---

## 🚀 Quick Start

### 1. Setup Database

Create a PostgreSQL database and run the schema script:

```bash
# Create database
createdb camus

# Run schema script
psql -U postgres -d camus -f src/Adapters/emc.camus.persistence.postgresql/schema.sql
```

### 2. Configure Application Settings

In `appsettings.json`:

```json
{
  "AppDataSettings": {
    "Provider": "Database",
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=camus;Username=postgres;Password=yourpassword"
    }
  },
  "Authorization": {
    "Provider": "Database",
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=camus;Username=postgres;Password=yourpassword"
    }
  }
}
```

**Environment Variables (Recommended for Production):**

```bash
AppDataSettings__Provider=Database
AppDataSettings__Database__ConnectionString="Host=prod-db.example.com;Database=camus;Username=app_user;Password=***"
Authorization__Provider=Database
Authorization__Database__ConnectionString="Host=prod-db.example.com;Database=camus;Username=app_user;Password=***"
```

### 3. Application Wiring

The repositories are automatically registered when you set the provider to `Database`. The API extensions handle the dependency injection:

```csharp
// In Program.cs (already configured)
builder.AddAppData();        // Registers PSApiInfoRepository
builder.AddAuthorization();  // Registers PSUserRepository

app.UseAppDataSetup();        // Initializes and validates database
app.UseAuthorizationSetup();  // Initializes and validates database
```

---

## 🏗️ Architecture

### Repository Implementations

#### PSApiInfoRepository

Manages API version information:

- `Initialize()` - Validates database connection and schema
- `GetByVersionAsync(version)` - Retrieves API info by version
- `GetAllAsync()` - Returns all API versions

#### PSUserRepository

Manages user authentication and authorization:

- `Initialize()` - Validates database connection and schema
- `ValidateCredentialsAsync(username, password)` - Authenticates users and loads roles

### Database Schema

```text
┌─────────────────┐      ┌─────────────────┐
│   api_info      │      │     roles       │
├─────────────────┤      ├─────────────────┤
│ id (PK)         │      │ id (PK)         │
│ name            │      │ name            │
│ version (UQ)    │      │ description     │
│ status          │      └────────┬────────┘
│ features[]      │               │
└─────────────────┘               │
                                  │
                      ┌───────────┴──────────┐
                      │                      │
         ┌────────────▼────────┐  ┌──────────▼──────────┐
         │ role_permissions    │  │   users             │
         ├─────────────────────┤  ├─────────────────────┤
         │ id (PK)             │  │ id (PK)             │
         │ role_id (FK)        │  │ username (UQ)       │
         │ permission          │  │ password_secret_name│
         └─────────────────────┘  └──────────┬──────────┘
                                             │
                                  ┌──────────▼──────────┐
                                  │   user_roles        │
                                  ├─────────────────────┤
                                  │ id (PK)             │
                                  │ user_id (FK)        │
                                  │ role_id (FK)        │
                                  └─────────────────────┘
```

---

## 📊 Database Schema Management

### Using the Provided Schema

The `schema.sql` file includes:

- ✅ Table creation with proper indexes
- ✅ Foreign key constraints
- ✅ Sample data for development
- ✅ Verification queries

```bash
# Apply schema
psql -U postgres -d camus -f schema.sql

# Verify tables
psql -U postgres -d camus -c "\dt"
```

### Schema Features

- **UUID Primary Keys** - Using `gen_random_uuid()` for distributed systems
- **Timestamps** - Automatic `created_at` and `updated_at` tracking
- **Indexes** - Optimized for common query patterns
- **Constraints** - Data integrity with foreign keys and unique constraints
- **Array Support** - PostgreSQL arrays for features and permissions

---

## 🔐 Security Considerations

### Password Storage

- ❌ **Never** store actual passwords in the database
- ✅ Store only secret references (`password_secret_name`)
- ✅ Retrieve actual passwords from secret provider (Dapr, Azure Key Vault, etc.)

```sql
-- Good: Store secret reference
INSERT INTO users (username, password_secret_name) 
VALUES ('admin', 'camus-admin-password');

-- Bad: Never do this
-- INSERT INTO users (username, password) VALUES ('admin', 'Password123!');
```

### Connection String Security

- Development: Use `appsettings.Development.json`
- Production: Use environment variables or secret management
- Never commit connection strings with real credentials

---

## ⚡ Advanced Usage

### Custom Queries

The repositories use Dapper for data access. Example of adding custom queries:

```csharp
public async Task<User?> GetUserByIdAsync(string userId)
{
    using var connection = await _connectionFactory.CreateConnectionAsync();
    
    const string sql = @"
        SELECT id, username, password_secret_name
        FROM users
        WHERE id = @UserId";
    
    return await connection.QuerySingleOrDefaultAsync<User>(
        sql, 
        new { UserId = userId });
}
```

### Transaction Support

Dapper supports transactions for multi-statement operations:

```csharp
using var connection = await _connectionFactory.CreateConnectionAsync();
using var transaction = connection.BeginTransaction();

try
{
    // Multiple operations
    await connection.ExecuteAsync(sql1, param1, transaction);
    await connection.ExecuteAsync(sql2, param2, transaction);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 🧪 Testing

### Integration Tests

For testing with PostgreSQL:

```csharp
public class PostgreSqlRepositoryTests : IDisposable
{
    private readonly IDbConnectionFactory _factory;
    
    public PostgreSqlRepositoryTests()
    {
        // Use test database connection
        var settings = new AppDataSettings
        {
            Database = new DatabaseSettings
            {
                ConnectionString = "Host=localhost;Database=camus_test;..."
            }
        };
        
        _factory = new NpgsqlConnectionFactory(settings, logger);
    }
    
    [Fact]
    public async Task GetByVersionAsync_ValidVersion_ReturnsApiInfo()
    {
        var repository = new PSApiInfoRepository(_factory, logger);
        repository.Initialize();
        
        var result = await repository.GetByVersionAsync("1.0");
        
        Assert.NotNull(result);
        Assert.Equal("1.0", result.Version);
    }
}
```

---

## 🔧 Troubleshooting

### Common Issues

#### Connection Errors

```text
Failed to open database connection
```

- Verify PostgreSQL is running: `pg_isready`
- Check connection string in settings
- Verify network access and firewall rules

#### Table Not Found

```text
Required table 'api_info' does not exist
```

- Run the schema script: `psql -d camus -f schema.sql`
- Verify you're connecting to the correct database

#### Secret Retrieval Errors

```text
Failed to retrieve password from secret 'xxx'
```

- Verify secret exists in secret provider
- Check secret provider configuration
- Ensure secret names match database records

---

## 📚 Related Documentation

- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

---

## 🤝 Contributing

When adding new repositories:

1. Create interface in `emc.camus.application`
2. Implement repository in this adapter
3. Create corresponding database tables in `schema.sql`
4. Update README with new tables and usage
5. Add integration tests

---

## 📝 License

Part of Camus API Template - See main repository for license information.

```csharp
public async Task<IEnumerable<Product>> GetAllAsync()
{
    const string sql = "SELECT * FROM products ORDER BY name";
    return await _connection.QueryAsync<Product>(sql);
}

public async Task<int> CreateAsync(Product product)
{
    const string sql = @"
        INSERT INTO products (name, price, description) 
        VALUES (@Name, @Price, @Description)
        RETURNING id";
    return await _connection.ExecuteScalarAsync<int>(sql, product);
}
```

### 4. Use Services

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;
    
    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Product> GetProductAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

---

## 🏗️ Clean Architecture

### Layer Separation

```text
┌──────────────────────────────────────┐
│         API Layer                    │
│       Controllers                    │
└───────────────┬──────────────────────┘
                │
┌───────────────▼──────────────────────┐
│      Application Layer               │
│    Services (Use Cases)              │
└───────────────┬──────────────────────┘
                │ depends on
┌───────────────▼──────────────────────┐
│   Application Interfaces (Ports)     │
│      IProductRepository              │
└───────────────┬──────────────────────┘
                │ implemented by
┌───────────────▼──────────────────────┐
│    Adapter Layer (Implementation)    │
│      ProductRepository               │
│      (uses Dapper + Npgsql)          │
└──────────────────────────────────────┘
```

---

## 📊 Schema Migrations

### Migrations (Recommended)

Use a migration tool for schema management:

### Option 1: FluentMigrator

```bash
dotnet add package FluentMigrator
dotnet add package FluentMigrator.Runner
```

### Option 2: EF Core Migrations (Schema only)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Example Schema

```sql
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_products_name ON products(name);
```

---

## ⚡ Advanced Patterns

### Multi-Statement Transactions

```csharp
public class OrderService
{
    private readonly IDbConnection _connection;
    
    public async Task CreateOrderWithItemsAsync(Order order, List<OrderItem> items)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            // Insert order
            const string orderSql = @"
                INSERT INTO orders (customer_id, total) 
                VALUES (@CustomerId, @Total) 
                RETURNING id";
            order.Id = await _connection.ExecuteScalarAsync<int>(orderSql, order, transaction);
            
            // Insert order items
            const string itemSql = @"
                INSERT INTO order_items (order_id, product_id, quantity, price) 
                VALUES (@OrderId, @ProductId, @Quantity, @Price)";
            await _connection.ExecuteAsync(itemSql, items, transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### Bulk Operations

```csharp
public async Task BulkInsertAsync(IEnumerable<Product> products)
{
    const string sql = @"
        INSERT INTO products (name, price, description) 
        VALUES (@Name, @Price, @Description)";
    
    await _connection.ExecuteAsync(sql, products);
}
```

---

## 🧪 Test Strategies

### Unit Tests (Mock Repository)

```csharp
var mockRepository = new Mock<IProductRepository>();
mockRepository
    .Setup(x => x.GetByIdAsync(1))
    .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

var service = new ProductService(mockRepository.Object);
var result = await service.GetProductAsync(1);

Assert.Equal("Test Product", result.Name);
```

### Integration Tests (Test Database)

```csharp
public class ProductRepositoryTests : IDisposable
{
    private readonly IDbConnection _connection;
    
    public ProductRepositoryTests()
    {
        _connection = new NpgsqlConnection("Host=localhost;Database=camus_test;...");
        _connection.Open();
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsProduct()
    {
        // Arrange
        var repository = new ProductRepository(_connection);
        
        // Act
        var product = await repository.GetByIdAsync(1);
        
        // Assert
        Assert.NotNull(product);
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

---

## 🔗 Related Documentation

- **[Architecture Guide](../../../../docs/architecture.md)** - Repository pattern and data layer
- **[Deployment Guide](../../../../docs/deployment.md)** - Database deployment strategies
- **[Dapper Documentation](https://github.com/DapperLib/Dapper)** - Dapper usage and examples

---

## 📦 Dependencies

- `Npgsql` - PostgreSQL .NET data provider
- `Dapper` - Micro-ORM for .NET
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection

---

## 🔧 Configuration Options

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=camus;Username=postgres;Password=pass"
  },
  "PostgreSql": {
    "CommandTimeout": 30,
    "MaxPoolSize": 100,
    "MinPoolSize": 5
  }
}
```

---

## ⚠️ Best Practices

- ✅ Use parameterized queries (Dapper does this automatically)
- ✅ Dispose connections properly (use `using` or DI)
- ✅ Use connection pooling (enabled by default)
- ✅ Implement retry logic for transient failures
- ✅ Use read replicas for read-heavy workloads
- ❌ Never concatenate SQL strings with user input
- ❌ Avoid N+1 query problems (use batch operations)
