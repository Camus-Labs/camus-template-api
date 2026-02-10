using emc.camus.ratelimiting.memory.Metrics;
using emc.camus.application.Generic;
using Microsoft.AspNetCore.Http;

namespace emc.camus.ratelimiting.memory.Middleware
{
    /// <summary>
    /// Middleware that records rate limiting metrics and adds RFC-compliant headers for successful requests.
    /// Implements IETF Draft Rate Limit Headers specification.
    /// Must be placed after UseRateLimiter() in the pipeline.
    /// </summary>
    public class RateLimitMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitMetrics _metrics;

        /// <summary>
        /// Initializes a new instance of the RateLimitMetricsMiddleware.
        /// </summary>
        public RateLimitMetricsMiddleware(RequestDelegate next, RateLimitMetrics metrics)
        {
            _next = next;
            _metrics = metrics;
        }

        /// <summary>
        /// Invokes the middleware to record metrics and add rate limit headers.
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

            // Record that this request passed rate limiting
            var endpoint = context.Request.Path;
            var method = context.Request.Method;

            // Record the hit (request passed rate limiting)
            _metrics.RecordHit(policy, endpoint, method);

            // Continue to next middleware
            await _next(context);
        }
    }
}
