# emc.camus.persistence.postgresql

PostgreSQL database adapter for Camus applications using Dapper and Npgsql.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

This adapter provides PostgreSQL database access using Dapper micro-ORM, implementing the repository
pattern for clean separation between data access and business logic. It supports both API information
and user authorization data persistence.

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

The repositories are automatically registered when you set the provider to `Database`. The API extensions handle the
dependency injection:

- `builder.AddAppData()` registers `PSApiInfoRepository`
- `builder.AddAuthorization()` registers `PSUserRepository`
- `app.UseAppDataSetup()` initializes and validates the database
- `app.UseAuthorizationSetup()` initializes and validates the database

See `AppDataSetupExtensions` and `AuthorizationSetupExtensions` in `src/Api/emc.camus.api/Extensions/` for the
wiring details.

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

Store only secret reference names (e.g., `password_secret_name`) in the database — never store raw passwords. Retrieve
actual credentials from the secret provider at runtime.

### Connection String Security

- Development: Use `appsettings.Development.json`
- Production: Use environment variables or secret management
- Never commit connection strings with real credentials

---

## ⚡ Advanced Usage

### Custom Queries

The repositories use Dapper for data access.

Add custom queries using Dapper's `QueryAsync`, `QuerySingleOrDefaultAsync`, and `ExecuteAsync` extension
methods on `IDbConnection`.

See the existing repository implementations in this adapter for query patterns.

### Transaction Support

Dapper supports transactions for multi-statement operations. Open a connection, begin a transaction,
pass it to each `Execute`/`Query` call, and commit or rollback as appropriate.

---

## 🧪 Testing

### Integration Tests

For testing with PostgreSQL, create a test database and use the adapter's connection factory pointed
at that database. Initialize the repository, then assert on query results.

See test projects in `src/Test/` for integration test patterns.

---

## Integration

This adapter integrates through extension methods defined in the API layer:

- `builder.AddAppData()` — registers `PSApiInfoRepository` when AppData provider is `Database`
- `builder.AddAuthorization()` — registers `PSUserRepository` when Authorization provider is `Database`
- `app.UseAppDataSetup()` — initializes and validates the database connection at startup
- `app.UseAuthorizationSetup()` — initializes and validates the database connection at startup

See `AppDataSetupExtensions` and `AuthorizationSetupExtensions` in `src/Api/emc.camus.api/Extensions/` for the
full wiring details.

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

---

## 📦 Dependencies

- `Npgsql` - PostgreSQL .NET data provider
- `Dapper` - Micro-ORM for .NET
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection

---

## 🔧 Configuration Options

See the `appsettings.json` configuration example in the Quick Start section above for connection and pooling settings.

---

## ⚠️ Best Practices

- ✅ Use parameterized queries (Dapper does this automatically)
- ✅ Dispose connections properly (use `using` or DI)
- ✅ Use connection pooling (enabled by default)
- ✅ Implement retry logic for transient failures
- ✅ Use read replicas for read-heavy workloads
- ❌ Never concatenate SQL strings with user input
- ❌ Avoid N+1 query problems (use batch operations)
