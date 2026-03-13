# Database Infrastructure

This directory contains database migration scripts and infrastructure for the Camus application.

> **📖 Parent Documentation:** [Main README](../../../README.md) |
[Architecture Guide](../../../docs/architecture.md)

## Directory Structure

``` text

database/
├── README.md                           # This file
└── migrations/                         # Database migration scripts
    ├── 001_initial_schema.sql         # Initial schema with API info and authorization
    └── ...                            # Future migrations (002, 003, etc.)
```

## Migration Naming Convention

Migrations follow the naming pattern: `{number}_{description}.sql`

- **Number**: Three-digit sequential number (001, 002, 003, etc.)
- **Description**: Brief snake_case description of the migration
- **Extension**: Always `.sql`

Examples:

- `001_initial_schema.sql`
- `002_add_user_preferences.sql`
- `003_add_audit_indexes.sql`

## Applying Migrations

### Development (Docker)

Migrations are automatically applied when Docker containers start for the first time. The initial schema is
mounted in the PostgreSQL container's `docker-entrypoint-initdb.d/` directory.

To reset and reapply migrations:

```bash
# Stop and remove containers with volumes
docker-compose -f docker-compose.dev.yml down -v

# Start fresh (migrations auto-apply)
docker-compose -f docker-compose.dev.yml up postgres -d
```

### Manual Application

To manually apply a migration:

```bash
# Connect to the database
psql -U camus -d camus -h localhost -p 5432

# Or apply from file
psql -U camus -d camus -h localhost -p 5432 -f migrations/001_initial_schema.sql
```

### Production (CI/CD Pipeline)

Migrations should be applied as part of your deployment pipeline (GitHub Actions, Azure DevOps, etc.):

```yaml
# Example GitHub Actions step
- name: Apply Database Migrations
  run: |
    # Using a migration tool (recommended)
    flyway migrate -url=${{ secrets.DB_URL }}
    
    # OR using psql directly (simple approach)
    psql ${{ secrets.DB_CONNECTION_STRING }} -f src/Infrastructure/database/migrations/*.sql
```

**Best Practices:**

1. Run migrations **before** deploying the new application version
2. Test in staging environment first
3. Use a migration tracking system (see Migration Versioning below)
4. Always backup database before applying migrations

## Migration Best Practices

1. **Idempotent**: Use `IF NOT EXISTS` clauses to make migrations repeatable
2. **Sequential**: Number migrations sequentially to maintain order
3. **Documented**: Include header comments explaining purpose and date
4. **Tested**: Test migrations in dev environment before production
5. **Reversible**: Consider rollback strategy (future: add down migrations)
6. **Atomic**: Keep each migration focused on a single logical change

## Database Schema

All tables are organized under the **`camus`** schema:

### Tables

- `camus.api_info` - API version information and features
- `camus.users` - User accounts with bcrypt password hashes
- `camus.roles` - Role definitions (Admin, ReadWrite, ReadOnly, etc.)
- `camus.role_permissions` - Permission strings assigned to each role (e.g., "api.read", "api.write", "token.create")
- `camus.user_roles` - Many-to-many relationship between users and roles
- `camus.action_audit` - Business action audit trail with OpenTelemetry trace correlation

## Connection Strings

### Development (Docker)

```text
Host=postgres;Port=5432;Database=camus;Username=camus;Password=camus_dev_password
```

### Development (Local)

```text
Host=localhost;Port=5432;Database=camus;Username=camus;Password=camus_dev_password
```

### Production

Use environment-specific secrets management (Dapr, Azure Key Vault, etc.)

## Migration Versioning

To prevent migrations from running twice, you need a **migration history table**:

### Option 1: Simple Version Table (Recommended for Start)

Add to your migrations:

```sql
-- Track applied migrations
CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(50) PRIMARY KEY,
    applied_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Record this migration
INSERT INTO schema_migrations (version) 
VALUES ('001') 
ON CONFLICT (version) DO NOTHING;
```

In CI/CD, check before applying:

```bash
# Check if migration already applied
psql -c "SELECT version FROM schema_migrations WHERE version='002';"
# Only run if not found
```

### Option 2: Use Migration Tools (Recommended for Production)

Popular tools that handle versioning automatically:

- **Flyway** - Simple, widely used, SQL-based
- **Liquibase** - Supports rollbacks, multiple formats
- **DbUp** - .NET-focused migration tool
- **EF Core Migrations** - If using Entity Framework

These tools automatically:

- Track which migrations ran
- Apply only new migrations
- Provide rollback capabilities
- Integrate with CI/CD pipelines

### Current Approach

For now, migrations use `IF NOT EXISTS` clauses making them **idempotent** (safe to run multiple times). This
works for initial setup but won't handle schema changes properly.

**Recommendation:** Add a simple `schema_migrations` table when you create your second
migration (002).

## Future Enhancements

- [ ] Add schema_migrations table for version tracking
- [ ] Consider Flyway or DbUp for production
- [ ] Rollback/down migration scripts
- [ ] Automated migration validation in CI/CD
- [ ] Environment-specific seed data scripts

---

## Configuration

Database connection details are configured through `DatabaseSettings` in `appsettings.json`. Migration credentials
are configured in `DBUpSettings` with secret names resolved by `ISecretProvider` at runtime.
See [Migrations Adapter README](../../Adapters/emc.camus.migrations.dbup/README.md) for the complete configuration
reference.

---

## Integration

Migration scripts in `migrations/` are embedded into the `emc.camus.migrations.dbup` adapter assembly at
build time. The adapter executes them at application startup via DbUp.
See [Migrations Adapter README](../../Adapters/emc.camus.migrations.dbup/README.md) for the wiring pattern.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| Migration script not detected | File not in `migrations/` or not matching `*.sql` naming pattern |
| Schema already exists error | Migration missing `IF NOT EXISTS` guard |
| Permission denied | Database user lacks DDL privileges — use admin credentials for migrations |
