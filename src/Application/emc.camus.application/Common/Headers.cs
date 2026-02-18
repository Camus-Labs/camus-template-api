using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Common;

/// <summary>
/// Defines custom HTTP header names for the application.
/// For standard headers, use Microsoft.Net.Http.Headers.HeaderNames from the framework.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Headers
{
    /// <summary>
    /// Header name for API Key authentication.
    /// </summary>
    public const string ApiKey = "Api-Key";

    /// <summary>
    /// Header name for distributed tracing correlation ID.
    /// </summary>
    public const string TraceId = "Trace-Id";

    /// <summary>
    /// Header name for authenticated user identification.
    /// </summary>
    public const string UserId = "User-Id";

    /// <summary>
    /// Header name for rate limit maximum requests allowed.
    /// </summary>
    public const string RateLimitLimit = "RateLimit-Limit";

    /// <summary>
    /// Header name for rate limit reset timestamp (Unix epoch seconds).
    /// </summary>
    public const string RateLimitReset = "RateLimit-Reset";

    /// <summary>
    /// Header name for retry-after seconds when rate limited.
    /// </summary>
    public const string RetryAfter = "Retry-After";

    /// <summary>
    /// Header name for rate limit policy name applied.
    /// </summary>
    public const string RateLimitPolicy = "RateLimit-Policy";

    /// <summary>
    /// Header name for rate limit window duration in seconds.
    /// </summary>
    public const string RateLimitWindow = "RateLimit-Window";
}
