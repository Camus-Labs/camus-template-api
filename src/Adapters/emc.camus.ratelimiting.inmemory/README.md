# emc.camus.ratelimiting.inmemory

In-memory rate limiting adapter for the Camus application using ASP.NET Core's built-in sliding window rate limiter.

> **📖 Parent Documentation:** [Main README](../../../README.md) | [Architecture Guide](../../../docs/architecture.md)

## Overview

This adapter provides IP-based rate limiting with policy-based configuration. It runs **before authentication**
to protect auth endpoints from brute force attacks.

## Features

- ✅ **Policy-based configuration** - Define multiple rate limit policies (strict, default, relaxed)
- ✅ **Sliding window algorithm** - Smooth rate distribution with configurable segments
- ✅ **IP-based limiting** - Handles X-Forwarded-For and X-Real-IP headers for proxy support
- ✅ **Attribute-based control** - Apply different policies per endpoint using `[RateLimit]` attribute
- ✅ **RFC-compliant headers** - Implements IETF Draft Rate Limit Headers specification
- ✅ **Custom exception handling** - Type-safe `RateLimitExceededException` with detailed context
- ✅ **Clean architecture** - Abstractions in Application layer, implementation in Adapter
- ✅ **Comprehensive observability** - Metrics, logging, and response headers
- ✅ **Configuration validation** - Early fail-fast on invalid settings

## Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                     API Request                              │
└────────────────────────────┬────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  Rate Limiter   │ ◄─── Runs BEFORE auth
                    │   (IP-based)    │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  Authentication │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   Controllers   │
                    └─────────────────┘
```

## Installation

**1. Add reference to your API project:**

```bash
dotnet add reference ../../Adapters/emc.camus.ratelimiting.inmemory/emc.camus.ratelimiting.inmemory.csproj
```

**2. Configure in `Program.cs`:**

1. Call `builder.AddInMemoryRateLimiting(serviceName)` to register rate limiting services
2. Call `app.UseForwardedHeaders()` to process proxy headers (must come before rate limiting)
3. Call `app.UseInMemoryRateLimiting()` to apply the middleware before authentication

See `InMemoryRateLimitingSetupExtensions` in this adapter for the full registration API and `Program.cs` for
the wiring order.

⚠️ **Critical**: If deploying behind a reverse proxy, `UseForwardedHeaders()` must be called **before**
`UseInMemoryRateLimiting()`. Without it, all requests from the same proxy share one rate limit.

## Configuration

Add to `appsettings.json`:

```json
{
  "InMemoryRateLimitingSettings": {
    "Policies": {
      "default": {
        "PermitLimit": 250,
        "WindowSeconds": 60
      },
      "strict": {
        "PermitLimit": 50,
        "WindowSeconds": 60
      },
      "relaxed": {
        "PermitLimit": 500,
        "WindowSeconds": 60
      }
    },
    "ExemptPaths": [ "/health", "/ready", "/alive", "/swagger" ]
  }
}
```

## Usage

### Apply to Controllers

Apply the `[RateLimit]` attribute from the Application layer to controllers or individual endpoints.
Use `RateLimitPolicies.Strict`, `RateLimitPolicies.Default`, or `RateLimitPolicies.Relaxed` constants.
Controller-level attributes are inherited by all endpoints unless overridden.

See the `RateLimitAttribute` and `RateLimitPolicies` in `src/Application/emc.camus.application/RateLimiting/`
for the available policies.

## Limitations

⚠️ **Single-Instance Only** - This adapter uses in-memory storage and is **NOT suitable for multi-instance
deployments**.

For production environments with horizontal scaling (Kubernetes, Azure App Service scale-out), replace this
adapter with a Redis-backed implementation that shares state across instances.

⚠️ **Proxy Header Detection** - IP resolution depends on reverse proxy configuration:

| Environment | Expected Headers | Configuration Required |
| ----------- | --------------- | ---------------------- |
| **Development/Testing** | None (direct connection) | ✅ No action needed |
| **Nginx** | X-Forwarded-For | ⚠️ Must add `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;` |
| **HAProxy** | X-Forwarded-For | ⚠️ Must enable `option forwardfor` |
| **Azure/AWS/GCP Load Balancers** | X-Forwarded-For | ✅ Automatic |
| **CloudFlare CDN** | CF-Connecting-IP + X-Forwarded-For | ✅ Automatic |

**Without proxy headers**: All requests from the same proxy IP share one rate limit (security risk in production).

The adapter logs a warning on first request if no proxy headers are detected. Review logs and ensure
`UseForwardedHeaders()` is configured in [Program.cs](../../Api/emc.camus.api/Program.cs#L58-L62).

## Metrics

Exports OpenTelemetry metrics for anomaly detection:

- `rate_limit_rejections_total` - Requests rejected due to rate limiting (signals attacks or misbehaving clients)

Tagged with: `policy`, `method`

**Note**: Success cases are not metered to avoid high-volume noise. Rate limit information for successful
requests is available via response headers (`RateLimit-Limit`, `RateLimit-Reset`).

## Response Headers

The adapter adds RFC-compliant IETF Draft Rate Limit Headers to **all responses** (both 200 OK and 429 Too
Many Requests).

**Why headers on all responses?**

- **Client visibility**: Clients know their limits proactively before hitting them
- **Intelligent retry logic**: Clients can implement exponential backoff based on actual limits
- **Usage tracking**: Clients can monitor their request usage and plan accordingly
- **Industry standard**: Follows practice of GitHub, Twitter, Stripe, and other major APIs

### Success Response (200 OK)

Includes rate limit headers so clients can track usage proactively.

### Rate Limited Response (429 Too Many Requests)

Includes `Retry-After` header and an RFC 7807 Problem Details body with the `rate_limit_exceeded` error code.

**Response Headers:**

| Header | Description |
| ------ | ----------- |
| `RateLimit-Limit` | Maximum requests allowed in the window |
| `RateLimit-Reset` | Unix timestamp when the window resets |
| `Retry-After` | Seconds until the client can retry (429 only) |
| `X-RateLimit-Policy` | Name of the applied rate limit policy |
| `X-RateLimit-Window` | Duration of the rate limit window in seconds |

## Clean Architecture

Following clean architecture principles:

```text
┌──────────────────────────────────────────────────┐
│              API Layer                            │
│  - Controllers with [RateLimit] attributes       │
│  - ExceptionHandlingMiddleware                   │
│  - References: Application, Adapter              │
└───────────────────┬──────────────────────────────┘
                    │
         ┌──────────▼──────────┐
         │  Application Layer  │
         │  - RateLimitAttribute│  ◄─── Endpoint metadata
         │  - RateLimitExceededException │
         │  - RateLimitPolicies │  ◄─── Shared constants
         └──────────┬──────────┘
                    │
         ┌──────────▼──────────┐
         │   Adapter Layer     │
         │  - InMemoryRateLimitingSettings│
         │  - ClientIpResolver │
         │  - Implementation   │
         └─────────────────────┘
```

**Dependency Flow**: API → Adapter (implementation) ← Application (abstractions)

**Key Benefits**:

- Type-safe attribute lookup (no reflection)
- Custom exception type with full context
- RFC-compliant headers for client compatibility
- Easy to swap implementations (memory → Redis → distributed cache)

---

## Integration

The adapter registers rate limiting services via two extension methods in `InMemoryRateLimitingSetupExtensions.cs`:

1. **`builder.AddInMemoryRateLimiting(serviceName)`** — Reads `InMemoryRateLimitingSettings` from configuration,
   validates policies, and registers the ASP.NET Core sliding-window rate limiter with IP-based partitioning.
2. **`app.UseInMemoryRateLimiting()`** — Activates the rate limiting middleware. Must be called **before**
   authentication middleware and **after** `UseForwardedHeaders()` when behind a proxy.

---

## Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| All requests share one rate limit | `UseForwardedHeaders()` not called before rate limiting when behind a proxy |
| 429 responses with default policy | No `[RateLimit]` attribute on endpoint — falls back to `default` policy |
| `InMemoryRateLimitingSettings configuration is missing` | Missing `InMemoryRateLimitingSettings` section in `appsettings.json` |
| Rate limit not applied to endpoint | Endpoint path matches an entry in `ExemptPaths` |
| Metrics not appearing | OpenTelemetry adapter not registered or meter name mismatch |

---

## Dependencies

- `Microsoft.AspNetCore.RateLimiting` - ASP.NET Core built-in rate limiter
- `emc.camus.application` - Application layer abstractions (RateLimitPolicies, ErrorCodes, MeterNames)

**Dependency Flow**: API → Adapter (setup) + Application (constants)

## Related Documentation

- [Architecture Guide](../../../docs/architecture.md)
- [Authentication Guide](../../../docs/authentication.md)

---

**Note:** This is a production-ready implementation for single-instance deployments. For distributed systems,
replace this adapter with a Redis-backed implementation.
