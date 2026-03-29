using System.Globalization;
using emc.camus.application.Common;
using Microsoft.AspNetCore.Http;

namespace emc.camus.ratelimiting.inmemory.Middleware
{
    /// <summary>
    /// Middleware that adds RFC-compliant rate limit headers to all responses.
    /// Implements IETF Draft Rate Limit Headers specification for client visibility.
    /// Must be placed after UseRateLimiter() in the pipeline.
    /// </summary>
    /// <remarks>
    /// Headers are added to ALL responses (not just 429) to allow clients to:
    /// - Know their rate limits proactively
    /// - Implement intelligent retry logic
    /// - Track usage and plan requests accordingly
    /// This follows industry standard practice (GitHub, Twitter, Stripe APIs).
    /// </remarks>
    internal sealed class RateLimitHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the RateLimitHeadersMiddleware.
        /// </summary>
        public RateLimitHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware to add rate limit headers to the response.
        /// Adds both RFC-compliant headers and custom headers for backward compatibility.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Retrieve partition info stored during rate limiter execution
            var policy = context.Items["RateLimit:Policy"]?.ToString() ?? "unknown";
            var limit = context.Items["RateLimit:Limit"]?.ToString() ?? "unknown";
            var window = context.Items["RateLimit:Window"]?.ToString() ?? "unknown";

            var headers = context.Response.Headers;

            // Add RFC-compliant IETF Draft headers
            // See: https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/
            headers[Headers.RateLimitLimit] = limit;

            // Calculate reset timestamp (current time + window)
            if (int.TryParse(window, out var windowSeconds))
            {
                var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(windowSeconds).ToUnixTimeSeconds();
                headers[Headers.RateLimitReset] = resetTimestamp.ToString(CultureInfo.InvariantCulture);
            }

            // Add custom headers for additional context (backward compatibility)
            headers[Headers.RateLimitPolicy] = policy;
            headers[Headers.RateLimitWindow] = window;

            // Continue to next middleware
            await _next(context);
        }
    }
}
