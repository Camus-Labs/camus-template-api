using emc.camus.application.Generic;
using Microsoft.AspNetCore.Http;

namespace emc.camus.ratelimiting.memory.Middleware
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
    public class RateLimitHeadersMiddleware
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

            // Add RFC-compliant IETF Draft headers
            // See: https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/
            context.Response.Headers[Headers.RateLimitLimit] = limit;
            
            // Calculate reset timestamp (current time + window)
            if (int.TryParse(window, out var windowSeconds))
            {
                var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(windowSeconds).ToUnixTimeSeconds();
                context.Response.Headers[Headers.RateLimitReset] = resetTimestamp.ToString();
            }

            // Add custom headers for additional context (backward compatibility)
            context.Response.Headers[Headers.RateLimitPolicy] = policy;
            context.Response.Headers[Headers.RateLimitWindow] = window;

            // Continue to next middleware
            await _next(context);
        }
    }
}
