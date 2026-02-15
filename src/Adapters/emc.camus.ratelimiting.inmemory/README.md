# emc.camus.ratelimiting.inmemory

In-memory rate limiting adapter for the Camus application using ASP.NET Core's built-in sliding window rate limiter.

## Overview

This adapter provides IP-based rate limiting with policy-based configuration. It runs **before authentication** to protect auth endpoints from brute force attacks.

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

```csharp
using emc.camus.ratelimiting.inmemory;
using Microsoft.AspNetCore.HttpOverrides;

// Step 1: Add rate limiting services
builder.AddMemoryRateLimiting(SERVICE_NAME);

var app = builder.Build();

// Step 2: Configure forwarded headers (REQUIRED for proxies)
// Must be BEFORE rate limiting to process X-Forwarded-For headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Step 3: Apply rate limiting middleware (BEFORE authentication)
app.UseMemoryRateLimiting();

app.UseAuthentication();
app.UseAuthorization();
```

⚠️ **Critical**: If deploying behind a reverse proxy, `UseForwardedHeaders()` must be called **before** `UseMemoryRateLimiting()`. Without it, all requests from the same proxy share one rate limit.

## Configuration

Add to `appsettings.json`:

```json
{
  "RateLimitSettings": {
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

```csharp
using emc.camus.application.RateLimiting;

// Apply strict policy to entire controller
[RateLimit(RateLimitPolicies.Strict)]
public class AuthController : ControllerBase
{
    // All endpoints inherit strict policy (10 req/min)
    
    [HttpPost("token")]
    public IActionResult GenerateToken() { ... }
    
    // Override with relaxed policy for specific endpoint
    [RateLimit(RateLimitPolicies.Relaxed)]
    [HttpGet("info")]
    public IActionResult GetInfo() { ... }
}
```

## Limitations

⚠️ **Single-Instance Only** - This adapter uses in-memory storage and is **NOT suitable for multi-instance deployments**.

For production environments with horizontal scaling (Kubernetes, Azure App Service scale-out), use the Redis adapter instead:

```bash
dotnet add package emc.camus.ratelimiting.redis
```

⚠️ **Proxy Header Detection** - IP resolution depends on reverse proxy configuration:

| Environment | Expected Headers | Configuration Required |
| ----------- | --------------- | ---------------------- |
| **Development/Testing** | None (direct connection) | ✅ No action needed |
| **Nginx** | X-Forwarded-For | ⚠️ Must add `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;` |
| **HAProxy** | X-Forwarded-For | ⚠️ Must enable `option forwardfor` |
| **Azure/AWS/GCP Load Balancers** | X-Forwarded-For | ✅ Automatic |
| **CloudFlare CDN** | CF-Connecting-IP + X-Forwarded-For | ✅ Automatic |

**Without proxy headers**: All requests from the same proxy IP share one rate limit (security risk in production).

The adapter logs a warning on first request if no proxy headers are detected. Review logs and ensure `UseForwardedHeaders()` is configured in [Program.cs](../../Api/emc.camus.api/Program.cs#L58-L62).

## Metrics

Exports OpenTelemetry metrics for anomaly detection:

- `rate_limit_rejections_total` - Requests rejected due to rate limiting (signals attacks or misbehaving clients)
- `rate_limit_undefined_policy_total` - Requests using undefined policy (configuration error)

Tagged with: `partition`, `endpoint`, `method`, `user_or_ip`

**Note**: Success cases are not metered to avoid high-volume noise. Rate limit information for successful requests is available via response headers (`RateLimit-Limit`, `RateLimit-Reset`).

## Migration to Redis

To migrate to Redis-based distributed rate limiting:

1. Install Redis adapter

   ```bash
   dotnet add package emc.camus.ratelimiting.redis
   ```

2. Update `Program.cs`

   ```csharp
   // Before
   builder.AddMemoryRateLimiting(SERVICE_NAME);
   app.UseMemoryRateLimiting();
   
   // After
   builder.AddRedisRateLimiting(SERVICE_NAME);
   app.UseRedisRateLimiting();
   ```

3. Add Redis connection string to configuration

   ```json
   {
     "RateLimitSettings": {
       "RedisConnectionString": "localhost:6379",
       "Policies": { }
     }
   }
   ```

## Response Headers

The adapter adds RFC-compliant IETF Draft Rate Limit Headers to **all responses** (both 200 OK and 429 Too Many Requests).

**Why headers on all responses?**

- **Client visibility**: Clients know their limits proactively before hitting them
- **Intelligent retry logic**: Clients can implement exponential backoff based on actual limits
- **Usage tracking**: Clients can monitor their request usage and plan accordingly
- **Industry standard**: Follows practice of GitHub, Twitter, Stripe, and other major APIs

### Success Response (200 OK)

```http
RateLimit-Limit: 100
RateLimit-Reset: 1640000000
X-RateLimit-Policy: default
X-RateLimit-Window: 60
```

### Rate Limited Response (429 Too Many Requests)

```http
RateLimit-Limit: 100
RateLimit-Reset: 1640000000
Retry-After: 60
X-RateLimit-Policy: default
X-RateLimit-Window: 60

{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded for policy 'default'. Limit: 100 requests per 60 seconds. Retry after 60 seconds.",
  "instance": "/api/v1/auth/token",
  "error": "rate_limit_exceeded",
  "retryAfter": 60,
  "policy": "default"
}
```

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
         │  - IRateLimiter     │  ◄─── Abstraction
         │  - RateLimitAttribute│
         │  - RateLimitExceededException │
         │  - RateLimitPolicies │  ◄─── Shared constants
         └──────────┬──────────┘
                    │
         ┌──────────▼──────────┐
         │   Adapter Layer     │
         │  - RateLimitSettings│
         │  - ClientIpResolver │
         │  - Implementation   │
         └─────────────────────┘
```

**Dependency Flow**: API → Adapter (implementation) ← Application (abstractions)

**Key Benefits**:

- Type-safe attribute lookup (no reflection)
- Custom exception type with full context
- RFC-compliant headers for client compatibility
- Testable abstraction (IRateLimiter)
- Easy to swap implementations (memory → Redis → distributed cache)

## Dependencies

- `Microsoft.AspNetCore.RateLimiting` - ASP.NET Core built-in rate limiter
- `emc.camus.application` - Application layer abstractions (RateLimitPolicies, ErrorCodes, MeterName

**Dependency Flow**: API → Adapter (setup) + Application (constants)

## Related Documentation

- [Architecture Guide](../../../../docs/architecture.md)
- [Authentication Guide](../../../../docs/authentication.md)

---

**Note:** This is a production-ready implementation for single-instance deployments. For distributed systems, migrate to the Redis adapter.
