# emc.camus.ratelimiting.inmemory

In-memory rate limiting adapter for the Camus application using ASP.NET Core's built-in sliding window rate limiter.

> **📖 Parent Documentation:** [Main README](../../../README.md) |
[Architecture — Cross-Cutting Concerns](../../../docs/architecture.md#cross-cutting-concerns) |
[Deployment — Security Checklist](../../../docs/deployment.md#security-checklist)

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

## Integration

**1. Add reference to your API project:**

Add a `<ProjectReference>` element targeting this adapter's `.csproj` to your API project file.

**2. Register services in `Program.cs`:**

1. Call `builder.AddInMemoryRateLimiting(serviceName)` to register rate limiting services
2. Call `app.UseForwardedHeaders()` to process proxy headers (must come before rate limiting)
3. Call `app.UseInMemoryRateLimiting()` to apply the middleware before authentication

See `InMemoryRateLimitingSetupExtensions` in this adapter for the full registration API and `Program.cs` for
the wiring order.

**Middleware ordering:**

- `UseForwardedHeaders()` → `UseInMemoryRateLimiting()` → `UseAuthentication()` → Controllers
- The `ExceptionHandlingMiddleware` in the API layer catches `RateLimitExceededException` and returns a
  429 response with RFC 7807 Problem Details body.

**Application layer interaction:**

- Endpoints declare their policy via `[RateLimit(RateLimitPolicies.Strict)]` (or `Default`/`Relaxed`).
- The adapter reads this attribute at runtime to select the partition policy.

⚠️ **Critical**: If deploying behind a reverse proxy, `UseForwardedHeaders()` must be called **before**
`UseInMemoryRateLimiting()`. Without it, the adapter reads `X-Forwarded-For` without trusted-proxy
validation, allowing clients to spoof IPs and bypass rate limits.

## Configuration

Add to `appsettings.json`:

```json
{
  "InMemoryRateLimitingSettings": {
    "SegmentsPerWindow": 5,
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

| Key | Default | Range | Description |
| --- | ------- | ----- | ----------- |
| `SegmentsPerWindow` | 5 | 1–20 | Number of segments in the sliding window. Higher values provide smoother limiting but use more memory. |
| `Policies` | 3 built-in | — | Named policies defining `PermitLimit` and `WindowSeconds`. A `default` policy is required. |
| `ExemptPaths` | 4 paths | — | Path prefixes exempt from rate limiting (case-insensitive, matched with StartsWith). |

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
| **Nginx** | X-Forwarded-For | ⚠️ Configure forwarding — see [Nginx proxy docs](https://nginx.org/en/docs/http/ngx_http_proxy_module.html#proxy_set_header) |
| **HAProxy** | X-Forwarded-For | ⚠️ Enable forwarding — see [HAProxy docs](https://www.haproxy.com/documentation/) |
| **Azure/AWS/GCP Load Balancers** | X-Forwarded-For | ✅ Automatic |

**Without proxy headers**: All requests from the same proxy IP share one rate limit (security risk in production).

The adapter logs a warning when an invalid IP format is found in the `X-Forwarded-For` header, which may
indicate header tampering or proxy misconfiguration. Ensure `UseForwardedHeaders()` is called before rate limiting
— see `UseTransportSecurity()` in [TransportSecuritySetupExtensions.cs](../../Api/emc.camus.api/Extensions/TransportSecuritySetupExtensions.cs).

## Metrics

Exports OpenTelemetry metrics for anomaly detection:

- `rate_limit_rejections_total` - Requests rejected due to rate limiting (signals attacks or misbehaving clients)

Tagged with: `policy`, `method`

**Note**: Success cases are not metered to avoid high-volume noise. Rate limit information for successful
requests is available via response headers (`RateLimit-Limit`, `RateLimit-Reset`).

## Response Headers

RFC-compliant IETF Draft Rate Limit Headers are added to **all responses** (both 200 OK and 429 Too
Many Requests). This adapter's `RateLimitHeadersMiddleware` sets headers on successful responses, while
the adapter's `HandleRateLimitRejection` callback sets all headers (including `Retry-After`) on 429 responses.

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

| Header | Description | Set By |
| ------ | ----------- | ------ |
| `RateLimit-Limit` | Maximum requests allowed in the window | Adapter |
| `RateLimit-Reset` | Unix timestamp when the window resets | Adapter |
| `RateLimit-Policy` | Name of the applied rate limit policy | Adapter |
| `RateLimit-Window` | Duration of the rate limit window in seconds | Adapter |
| `Retry-After` | Seconds until the client can retry (429 only) | Adapter |

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

**Dependency Flow**: API → Application ← Adapter (implements Application contracts)

**Key Benefits**:

- Type-safe attribute lookup (no reflection)
- Custom exception type with full context
- RFC-compliant headers for client compatibility
- Easy to swap implementations (memory → Redis → distributed cache)

## Troubleshooting

| Symptom | Cause | Resolution |
| ------- | ----- | ---------- |
| All requests share one rate limit behind a proxy | `UseForwardedHeaders()` not called before `UseInMemoryRateLimiting()` | Ensure `UseForwardedHeaders()` is placed earlier in the pipeline — see `UseTransportSecurity()` in the API layer. |
| `InvalidOperationException` at startup mentioning policies | Missing or invalid `default` policy, policy name not in allowed set, or `SegmentsPerWindow` outside 1–20 | Check `appsettings.json` against the Configuration section above. All policy names must be `default`, `strict`, or `relaxed`. |
| Warning log: "Invalid IP format in X-Forwarded-For header" | Malformed or spoofed `X-Forwarded-For` value from upstream proxy | Verify proxy configuration passes valid IPs. The adapter falls back to `X-Real-IP` then `RemoteIpAddress`. |
| Rate limits incorrect across multiple instances | In-memory storage is not shared between instances | Replace with a Redis-backed adapter for horizontally-scaled deployments. |

---

## Dependencies

- `Microsoft.AspNetCore.RateLimiting` - ASP.NET Core built-in rate limiter
- `emc.camus.application` - Application layer abstractions (RateLimitPolicies, ErrorCodes, MeterNames)

## Related Documentation

- [Architecture Guide](../../../docs/architecture.md)
- [Deployment Guide — Security Checklist](../../../docs/deployment.md#security-checklist)

---

**Note:** This is a production-ready implementation for single-instance deployments. For distributed systems,
replace this adapter with a Redis-backed implementation.
