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

Create a PostgreSQL database and apply schema migrations via the DbUp adapter:

```bash
# Create database
createdb camus
```

Schema is managed via the [DbUp migrations adapter](../emc.camus.migrations.dbup/README.md) — migration scripts run
automatically at application startup when `DBUpSettings.Enabled` is `true`.

### 2. Configure Application Settings

In `appsettings.json`:

```json
{
  "DataPersistenceSettings": {
    "Provider": "PostgreSQL"
  },
  "DatabaseSettings": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "camus",
    "UserSecretName": "DBUser",
    "PasswordSecretName": "DBSecret",
    "AdditionalParameters": "Timeout=30;Pooling=true;SslMode=Require"
  }
}
```

See [DatabaseSettings](../../../src/Application/emc.camus.application/Configurations/DatabaseSettings.cs)
for the full property reference.

### 3. Application Wiring

The repositories are registered via `PersistenceSetupExtensions` in the API layer:

- `builder.AddPersistence()` routes to `AddPostgreSqlPersistence()` when
  `DataPersistenceSettings.Provider` is `PostgreSQL`
- `app.UsePersistenceAsync()` initializes `AuthService` and `ApiInfoService`
  at startup

See `PersistenceSetupExtensions` in `src/Api/emc.camus.api/Extensions/` for the wiring details.

---

## 🏗️ Architecture

### Repository Implementations

#### ApiInfoRepository

Manages API version information:

- `InitializeAsync()` - Validates database connection and schema
- `GetByVersionAsync(version)` - Retrieves API info by version

#### UserRepository

Manages user authentication and authorization:

- `InitializeAsync()` - Validates database connection and schema
- `ValidateCredentialsAsync(username, password)` - Authenticates users and loads roles

---

## 📊 Database Schema Management

Schema is managed via the [DbUp migrations adapter](../emc.camus.migrations.dbup/README.md). Migration scripts live
in `src/Infrastructure/database/migrations/` and run automatically at application startup when `DBUpSettings.Enabled`
is `true`.

### Schema Features

- **UUID Primary Keys** - Using `gen_random_uuid()` for distributed systems
- **Timestamps** - Automatic `created_at` and `updated_at` tracking
- **Indexes** - Optimized for common query patterns
- **Constraints** - Data integrity with foreign keys and unique constraints
- **Array Support** - PostgreSQL arrays for features and permissions

---

## 🔐 Security Considerations

### Password Storage

Passwords are stored as bcrypt hashes in the `password_hash` column. The `UserRepository.ValidateCredentialsAsync`
method verifies credentials against the stored hash at runtime.

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

This adapter integrates through extension methods in the API layer:

- `builder.AddPersistence()` routes to `AddPostgreSqlPersistence()` when
  `DataPersistenceSettings.Provider` is `PostgreSQL`
- `app.UsePersistenceAsync()` initializes `AuthService` and `ApiInfoService`
  at startup

See `PersistenceSetupExtensions` in `src/Api/emc.camus.api/Extensions/` for the full wiring details.

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

- Run the schema script: see [DbUp migrations adapter](../emc.camus.migrations.dbup/README.md)
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
3. Add a new DbUp migration script for new tables
4. Update README with new tables and usage
5. Add integration tests

---

## 📝 License

Part of Camus API Template - See main repository for license information.

---

## 📦 Dependencies

- `Npgsql` - PostgreSQL .NET data provider
- `Dapper` - Micro-ORM for .NET
- `BCrypt.Net-Next` - Password hashing
- `Microsoft.Extensions.Logging.Abstractions` - Logging abstractions

---

## 🔧 Configuration Options

See [DatabaseSettings](../../../src/Application/emc.camus.application/Configurations/DatabaseSettings.cs)
for the full property reference.
