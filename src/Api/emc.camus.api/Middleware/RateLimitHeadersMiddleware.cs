using System.Globalization;
using emc.camus.api.Infrastructure;
using emc.camus.application.Common;
using Microsoft.AspNetCore.Http;

namespace emc.camus.api.Middleware;

/// <summary>
/// Middleware that adds RFC-compliant rate limit headers to all responses.
/// Implements IETF Draft Rate Limit Headers specification for client visibility.
/// Must be placed after UseRateLimiter() in the pipeline.
/// </summary>
public sealed class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the RateLimitHeadersMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="timeProvider">The time provider for reset calculations.</param>
    public RateLimitHeadersMiddleware(RequestDelegate next, TimeProvider timeProvider)
    {
        _next = next;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Invokes the middleware to add rate limit headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Retrieve partition info stored during rate limiter execution
        var policy = context.Items[RateLimitContextKeys.Policy]?.ToString() ?? "unknown";
        var limit = context.Items[RateLimitContextKeys.Limit]?.ToString() ?? "unknown";
        var window = context.Items[RateLimitContextKeys.Window]?.ToString() ?? "unknown";

        var headers = context.Response.Headers;

        // Add RFC-compliant IETF Draft headers
        headers[Headers.RateLimitLimit] = limit;

        // Calculate reset timestamp (current time + window)
        if (int.TryParse(window, out var windowSeconds))
        {
            var resetTimestamp = _timeProvider.GetUtcNow().AddSeconds(windowSeconds).ToUnixTimeSeconds();
            headers[Headers.RateLimitReset] = resetTimestamp.ToString(CultureInfo.InvariantCulture);
        }

        // Add custom headers for additional context (backward compatibility)
        headers[Headers.RateLimitPolicy] = policy;
        headers[Headers.RateLimitWindow] = window;

        // Continue to next middleware
        await _next(context);
    }
}
