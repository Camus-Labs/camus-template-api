# emc.camus.cache.inmemory

In-memory cache adapter for Camus applications providing token revocation and idempotency response caching
with `ConcurrentDictionary`.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

---

## Overview

This adapter implements the `ITokenRevocationCache` and `IIdempotencyResponseCache` interfaces from the Application
layer using thread-safe `ConcurrentDictionary` instances. It supports JWT token revocation by tracking revoked token
identifiers (JTI) and idempotency response caching with TTL-based expiration. Expired entries are cleaned up by a
background sync service (token revocation) or on retrieval (idempotency responses).

---

## Features

- **Thread-Safe** - Uses `ConcurrentDictionary` for safe concurrent access across requests
- **Background Sync** - `TokenRevocationSyncService` periodically reloads the token revocation cache from persistence
- **Idempotency Response Caching** - `IdempotencyResponseCache` stores and retrieves cached responses with TTL-based expiration
- **Singleton Lifetime** - Registered as a singleton, shared across all requests
- **Interface-Based** - Implements `ITokenRevocationCache` and `IIdempotencyResponseCache` from Application layer
- **Sensible Defaults** - Works out of the box with configurable sync interval

---

## Usage

### Register in Program.cs

Call `builder.AddInMemoryCache()` to register in-memory cache services. See `InMemoryCacheSetupExtensions` in this
adapter for the full registration API.

---

## Architecture

### Dependency Inversion

```text
┌──────────────────────────────────────────┐
│        Application Layer                 │
│  ITokenRevocationCache                   │
│  IIdempotencyResponseCache               │
└───────────────▲──────────────────────────┘
                │ implements
┌───────────────┴──────────────────────────┐
│         Adapter Layer                    │
│  TokenRevocationCache                    │
│  (implements ITokenRevocationCache)      │
│  IdempotencyResponseCache                │
│  (implements IIdempotencyResponseCache)  │
│  TokenRevocationSyncService              │
│  (background sync from persistence)      │
└──────────────────────────────────────────┘
```

**Benefits:**

- Application layer doesn't depend on any caching technology
- Easy to swap implementations (in-memory → Redis → distributed cache)
- Testable with mocked `ITokenRevocationCache` and `IIdempotencyResponseCache`

---

## Configuration

Add to `appsettings.json`:

```json
{
  "InMemoryCacheSettings": {
    "TokenRevocationCache": {
      "SyncEnabled": true,
      "SyncIntervalSeconds": 300
    }
  }
}
```

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `SyncEnabled` | bool | `true` | Enables background sync with persistence |
| `SyncIntervalSeconds` | int | `300` | Interval between sync cycles (10–86400) |

When `SyncEnabled` is `false`, the background sync service is not registered and the cache only contains tokens
revoked during the current application lifetime.

---

## Integration

The adapter registers via the extension method in `InMemoryCacheSetupExtensions.cs`:

- **`builder.AddInMemoryCache()`** — Loads and validates `InMemoryCacheSettings`, registers `TokenRevocationCache`
  as the `ITokenRevocationCache` singleton, registers `IdempotencyResponseCache` as the
  `IIdempotencyResponseCache` singleton, and conditionally registers `TokenRevocationSyncService` as a hosted
  background service when `SyncEnabled` is `true`.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| Revoked tokens still accepted after restart | In-memory store is cleared on restart — expected behavior |
| Revoked tokens accepted in other instances | In-memory cache is not shared across instances — use Redis adapter |
| Memory growing unexpectedly | Sync service may be disabled; enable `SyncEnabled` to periodically replace the cache |
| "No IGeneratedTokenRepository registered" log | Persistence adapter not registered — sync service requires a repository |

---

## Limitations

⚠️ **Single-Instance Only** — This adapter uses in-memory storage and is **NOT suitable for multi-instance
deployments**. Revoked tokens are not shared across application instances.

For production environments with horizontal scaling (Kubernetes, Azure App Service scale-out), use a
Redis-backed implementation instead.

⚠️ **No Persistence** — All revocation data is lost from in-memory storage when the application restarts. When
`SyncEnabled` is `true` and a persistence adapter is registered, the cache is repopulated from the repository on
startup. Without sync or persistence, previously revoked tokens will be accepted until they expire naturally.

---

## Related Documentation

- **[Authentication Guide](../../../docs/authentication.md)** — Complete authentication overview
- **[Architecture Guide](../../../docs/architecture.md)** — Cache architecture

---

## Dependencies

- `emc.camus.application` — Application interfaces (`ITokenRevocationCache`, `IGeneratedTokenRepository`)
- `Microsoft.AspNetCore.App` — ASP.NET Core framework reference
