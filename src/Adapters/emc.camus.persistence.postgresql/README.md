# emc.camus.persistence.postgresql

PostgreSQL database adapter for Camus applications using Dapper.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides PostgreSQL database access using Dapper micro-ORM, implementing the repository pattern for clean separation between data access and business logic.

---

## ✨ Features

- 🗄️ **PostgreSQL Integration** - Optimized for PostgreSQL with Npgsql
- ⚡ **Dapper Micro-ORM** - Lightweight, fast SQL mapping
- 🎯 **Repository Pattern** - Abstraction for data access
- 🔄 **Async/Await** - Non-blocking database operations
- ⚙️ **Connection Pooling** - Built-in connection management
- 🧪 **Testable** - Interface-based design

---

## 🚀 Usage

### 1. Register in Program.cs

```csharp
using emc.camus.persistence.postgresql;

builder.Services.AddPostgreSqlPersistence(builder.Configuration);
```

### 2. Configure Connection String

In `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=camus;Username=postgres;Password=yourpassword"
  }
}
```

**Environment Variable (Production):**

```bash
ConnectionStrings__DefaultConnection="Host=prod-db.postgres.database.azure.com;Database=camus;Username=admin;Password=***"
```

### 3. Implement Repository

```csharp
public interface IProductRepository
{
    Task<Product> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly IDbConnection _connection;
    
    public ProductRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<Product> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM products WHERE id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }
    
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
}
```

### 4. Use in Services

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

## 🏗️ Architecture

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

## 📊 Database Schema Management

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

### Transaction Support

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

## 🧪 Testing

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
