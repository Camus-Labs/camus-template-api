# emc.camus.cache.inmemory

In-memory cache adapter for Camus applications providing token revocation caching with `ConcurrentDictionary`.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

This adapter implements the `ITokenRevocationCache` interface from the Application layer using a thread-safe
`ConcurrentDictionary`. It supports JWT token revocation by tracking revoked token identifiers (JTI) with their
expiration timestamps, and performs lazy eviction of expired entries during lookups.

---

## ✨ Features

- 🔒 **Thread-Safe** - Uses `ConcurrentDictionary` for safe concurrent access across requests
- ⏱️ **Lazy Eviction** - Expired tokens are removed automatically during `IsRevoked` lookups
- 🎯 **Singleton Lifetime** - Registered as a singleton, shared across all requests
- 🔄 **Interface-Based** - Implements `ITokenRevocationCache` from Application layer
- ⚙️ **Zero Configuration** - No settings required; register and use

---

## 🚀 Usage

### Register in Program.cs

Call `builder.AddInMemoryCache()` to register in-memory cache services. See `InMemoryCacheSetupExtensions` in this
adapter for the full registration API.

---

## 🏗️ Architecture

### Dependency Inversion

```text
┌──────────────────────────────────────┐
│      Application Layer               │
│      ITokenRevocationCache           │
└───────────────┬──────────────────────┘
                │ depends on
┌───────────────▼──────────────────────┐
│       Adapter Layer                  │
│    IMTokenRevocationCache            │
│  (implements ITokenRevocationCache)  │
└──────────────────────────────────────┘
```

**Benefits:**

- Application layer doesn't depend on any caching technology
- Easy to swap implementations (in-memory → Redis → distributed cache)
- Testable with mocked `ITokenRevocationCache`

---

## ⚙️ Configuration

This adapter requires no configuration. It is registered as a singleton and stores revoked tokens in memory for the
lifetime of the application process.

---

## 🔗 Integration

The adapter registers via the extension method in `InMemoryCacheSetupExtensions.cs`:

- **`builder.AddInMemoryCache()`** — Registers `IMTokenRevocationCache` as the `ITokenRevocationCache` singleton in the
  DI container.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| Revoked tokens still accepted after restart | In-memory store is cleared on application restart — expected behavior |
| Revoked tokens accepted in other instances | In-memory cache is not shared across instances — use Redis adapter |
| Memory growing over time | Expired tokens are only evicted lazily during lookups; heavy revocation without lookups may accumulate entries until they expire |

---

## ⚠️ Limitations

⚠️ **Single-Instance Only** — This adapter uses in-memory storage and is **NOT suitable for multi-instance
deployments**. Revoked tokens are not shared across application instances.

For production environments with horizontal scaling (Kubernetes, Azure App Service scale-out), use a Redis-backed
implementation instead.

⚠️ **No Persistence** — All revocation data is lost when the application restarts. After a restart, previously revoked
tokens will be accepted until they expire naturally.

---

## 🔗 Related Documentation

- **[JWT Authentication Adapter](../emc.camus.security.jwt/README.md)** — Token generation and validation
- **[Authentication Guide](../../../docs/authentication.md)** — Complete authentication overview
- **[Architecture Guide](../../../docs/architecture.md)** — Cache architecture

---

## 📦 Dependencies

- `emc.camus.application` — Application interfaces (`ITokenRevocationCache`)
- `Microsoft.AspNetCore.App` — ASP.NET Core framework reference
