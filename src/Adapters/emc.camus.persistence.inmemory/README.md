# emc.camus.persistence.inmemory

In-memory persistence adapter for Camus applications providing configuration-driven repositories for development and
testing.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

This adapter implements the repository interfaces from the Application layer using in-memory data structures loaded from
`appsettings.json`. It provides a lightweight persistence layer suitable for development, testing, and single-instance
scenarios where a database is not required.

---

## ✨ Features

- 🗄️ **Configuration-Driven** — API info and user data loaded from `appsettings.json`
- 🔐 **Secret Provider Integration** — User credentials retrieved from `ISecretProvider`
- 📝 **Audit Logging** — No-op audit repository that logs to application logs instead of a database
- 🔄 **Interface-Based** — Implements `IUserRepository`, `IApiInfoRepository`, and `IActionAuditRepository`
- ⚙️ **Zero Infrastructure** — No database, no connection strings, no migrations

---

## 🚀 Usage

### Register in Program.cs

Call `builder.AddInMemoryPersistence()` to register all in-memory persistence services.
See `InMemoryPersistenceSetupExtensions` in this adapter for the full registration API.

---

## ⚙️ Configuration

The adapter reads from two configuration sections in `appsettings.json`:

### Data Persistence Provider (`DataPersistenceSettings`)

```json
{
  "DataPersistenceSettings": {
    "Provider": "InMemory"
  }
}
```

### In-Memory Models (`InMemoryModelSettings`)

```json
{
  "InMemoryModelSettings": {
    "Roles": [
      {
        "Name": "Admin",
        "Permissions": ["token.create", "api.read", "api.write"]
      },
      {
        "Name": "ReadOnly",
        "Permissions": ["api.read"]
      }
    ],
    "Users": [
      {
        "UsernameSecretName": "AdminUser",
        "PasswordSecretName": "AdminSecret",
        "Roles": ["Admin"]
      }
    ],
    "ApiInfos": [
      {
        "Name": "Camus API - 1.0 Release",
        "Version": "1.0",
        "Status": "Available",
        "Features": [
          "Basic API Information",
          "Public Endpoints"
        ]
      }
    ]
  }
}
```

> **Note:** User credentials (`UsernameSecretName`, `PasswordSecretName`) are resolved from `ISecretProvider` at
> startup — actual values are never stored in configuration files.
>
> **📖 Secrets Management:** See [Dapr Secrets Adapter](../emc.camus.secrets.dapr/README.md) for secret provider
> configuration.

---

## 🏗️ Architecture

### Dependency Inversion

```text
┌──────────────────────────────────────┐
│      Application Layer               │
│    IUserRepository                   │
│    IApiInfoRepository                │
│    IActionAuditRepository            │
└───────────────┬──────────────────────┘
                │ depends on
┌───────────────▼──────────────────────┐
│       Adapter Layer                  │
│    IMUserRepository                  │
│    IMApiInfoRepository               │
│    IMActionAuditRepository           │
└───────────────┬──────────────────────┘
                │ uses
┌───────────────▼──────────────────────┐
│  ISecretProvider + Configuration     │
│  (appsettings.json + secrets)        │
└──────────────────────────────────────┘
```

**Benefits:**

- Application layer doesn't depend on any persistence technology
- Easy to swap implementations (in-memory → PostgreSQL → other databases)
- Testable with mocked repository interfaces
- No infrastructure setup required for development

---

## 🔗 Integration

The adapter registers via the extension method in `InMemoryPersistenceSetupExtensions.cs`:

- **`builder.AddInMemoryPersistence()`** — Registers all in-memory
  repository implementations as singletons: `IUserRepository`,
  `IApiInfoRepository`, `IActionAuditRepository`, and `IUnitOfWork`.

Both `IMUserRepository` and `IMApiInfoRepository` require explicit initialization at startup via their `Initialize()`
methods to load data from configuration and secrets.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `Repository not initialized. Call Initialize() first.` | `Initialize()` not called at startup for the repository |
| `Failed to retrieve username from secret` | Secret name in `InMemoryModelSettings` doesn't match a secret in the store |
| `Duplicate API version` | Two entries in `ApiInfos` share the same `Version` value |
| `API info not found for version` | Requested version not defined in `InMemoryModelSettings.ApiInfos` |
| Data lost after restart | Expected behavior — in-memory storage does not persist across restarts |
| `IMApiInfoRepository already initialized` | `Initialize()` called more than once |

---

## ⚠️ Limitations

⚠️ **Single-Instance Only** — This adapter uses in-memory storage and is **NOT suitable for multi-instance
deployments**. Data is not shared across application instances.

⚠️ **No Persistence** — All data is loaded from configuration at startup and lost when the application restarts.
Runtime changes (e.g., last login updates) are not persisted.

⚠️ **Plain-Text Password Comparison** — The `IMUserRepository` compares passwords using direct string comparison
retrieved from the secret provider. In production, use the PostgreSQL adapter with proper password hashing.

---

## 🔗 Related Documentation

- **[Persistence (PostgreSQL)](../emc.camus.persistence.postgresql/README.md)** — Production database adapter
- **[Secrets (Dapr)](../emc.camus.secrets.dapr/README.md)** — Secret provider for user credentials
- **[Authentication Guide](../../../docs/authentication.md)** — Complete authentication overview
- **[Architecture Guide](../../../docs/architecture.md)** — Persistence architecture

---

## 📦 Dependencies

- `emc.camus.application` — Application interfaces (`IUserRepository`, `IApiInfoRepository`,
  `IActionAuditRepository`, `ISecretProvider`)
- `emc.camus.domain` — Domain entities (`User`, `Role`, `ApiInfo`)
- `Microsoft.Extensions.Logging.Abstractions` — Logging abstractions
- `Microsoft.AspNetCore.App` — ASP.NET Core framework reference
