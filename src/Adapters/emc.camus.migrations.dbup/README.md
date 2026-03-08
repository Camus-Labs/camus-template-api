# emc.camus.migrations.dbup

Database migration adapter for Camus applications using DbUp for PostgreSQL schema versioning.

> **📖 Parent Documentation:** [Main README](../../../../README.md) | [Architecture Guide](../../../../docs/architecture.md)

---

## 📋 Overview

Runs ordered, embedded SQL scripts against PostgreSQL at application startup. Tracks applied scripts in
`public.camus_schemaversions` to ensure each migration executes only once. Admin credentials are resolved at runtime via
`ISecretProvider` — never stored in configuration.

---

## ✨ Features

- 📦 **Embedded SQL Scripts** — Migration files compiled into the assembly from `src/Infrastructure/database/migrations/`
- 🔐 **Secret-Based Credentials** — Admin credentials fetched via `ISecretProvider` at startup
- 🔁 **Idempotent** — Already-applied scripts are skipped automatically
- 📋 **Schema Versioning** — Applied migrations tracked in `public.camus_schemaversions`
- 🚀 **Fail-Fast** — Configuration and credentials validated before migrations run
- 🧩 **Hexagonal Architecture** — No infrastructure coupling in the Application layer

---

## 🚀 Usage

### Wire up in `Program.cs`

```csharp
builder.AddDaprSecrets();          // Must run before migrations (provides ISecretProvider)
builder.AddDatabaseMigrations();   // Validates DBUpSettings + DatabaseSettings

app.UseDaprSecrets();              // Initialise secrets
app.UseDatabaseMigrations(logger); // Execute pending migrations
```

### Configuration (`appsettings.json`)

```json
{
  "DatabaseSettings": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "camus",
    "AdditionalParameters": "Timeout=30;Pooling=true;SslMode=Require"
  },
  "DBUpSettings": {
    "AdminSecretName": "DBMigrationsUser",
    "PasswordSecretName": "DBMigrationsSecret"
  }
}
```

`DBUpSettings` values are **secret names** in the secret store — not real credentials.

---

## ⚙️ Configuration Reference

| Setting | Required | Description |
| ------- | -------- | ----------- |
| `DBUpSettings.AdminSecretName` | ✅ | Secret name for DB admin username |
| `DBUpSettings.PasswordSecretName` | ✅ | Secret name for DB admin password |
| `DatabaseSettings.Host` | ✅ | PostgreSQL server hostname |
| `DatabaseSettings.Port` | ✅ | PostgreSQL server port |
| `DatabaseSettings.Database` | ✅ | Target database name |
| `DatabaseSettings.AdditionalParameters` | ❌ | Extra Npgsql connection string parameters |

---

## 📂 Migration Scripts

- **Location:** `src/Infrastructure/database/migrations/`
- **Embedded** into the assembly at build time — no `.csproj` changes needed for new files
- **Execution order:** Lexicographic by filename — use zero-padded numeric prefixes

```text
001_initial_schema.sql
002_generated_tokens.sql
003_next_change.sql       ← add new files here
```

> Variable substitution is **disabled** to avoid conflicts with `$` in bcrypt hashes.

**To add a migration:** create a new numbered `.sql` file. DbUp detects and applies it on the next startup. Never modify
or renumber an already-applied script — always add a new one.

---

## 🔐 Security

- Migration credentials (`DBMigrationsUser`) require **DDL privileges** (schema changes)
- Runtime app credentials (`DBUser`) should have **DML privileges only**
- Credentials are assembled in memory at runtime — never stored in config or logged

---

## 🏗️ Architecture

```text
API (Program.cs)
  └─ AddDatabaseMigrations / UseDatabaseMigrations
        └─ resolves ISecretProvider (Application port)
              └─ fetches admin credentials from secret store
                    └─ DbUp executes embedded .sql scripts
                          └─ tracks progress in public.camus_schemaversions
```

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `DBUpSettings configuration is missing` | Missing `DBUpSettings` section in config |
| `Secret 'DBMigrationsUser' not found` | Secret name mismatch or Dapr sidecar not running |
| `Database migration failed` | SQL error in a script — check logs for the failing statement |
| `Failed to run database migrations` | PostgreSQL unreachable — verify host, port, and firewall |

---

## 📚 Related Documentation

- **[Dapr Secrets Adapter](../emc.camus.secrets.dapr/README.md)** — `ISecretProvider` implementation
- **[PostgreSQL Persistence Adapter](../emc.camus.persistence.postgresql/README.md)** — Runtime DB access
- **[Architecture Guide](../../../../docs/architecture.md)** — Hexagonal architecture overview
- **[DbUp Documentation](https://dbup.readthedocs.io/)** — DbUp reference

---

## 📦 Dependencies

| Package | Purpose |
| ------- | ------- |
| `dbup-postgresql` | DbUp PostgreSQL support |
| `Microsoft.Extensions.Logging.Abstractions` | Logging integration |
| `emc.camus.application` | `ISecretProvider`, `DatabaseSettings` contracts |
