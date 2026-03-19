using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using emc.camus.ratelimiting.inmemory.Configurations;
using emc.camus.ratelimiting.inmemory.Metrics;
using emc.camus.ratelimiting.inmemory.Middleware;
using emc.camus.ratelimiting.inmemory.Services;
using emc.camus.application.Exceptions;
using emc.camus.application.RateLimiting;
using emc.camus.application.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace emc.camus.ratelimiting.inmemory
{
    /// <summary>
    /// Provides extension methods for configuring in-memory rate limiting.
    /// Uses ASP.NET Core's built-in rate limiting with sliding window algorithm.
    ///
    /// ⚠️ WARNING: This implementation uses in-memory storage and is NOT suitable for
    /// multi-instance deployments. For production scale-out scenarios, use the Redis adapter.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static partial class InMemoryRateLimitingSetupExtensions
    {
        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Rate limit policy '{PolicyName}' not found in configuration. Falling back to '{DefaultPolicy}' policy. Endpoint: {Method} {Path}")]
        private static partial void LogPolicyNotFound(ILogger logger, string policyName, string defaultPolicy, string method, string path);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Rate limit exceeded for IP: {IpAddress}, Policy: {Policy}, Endpoint: {Method} {Path}, Limit: {Limit} req/{Window}s")]
        private static partial void LogRateLimitExceeded(ILogger logger, string ipAddress, string policy, string method, string path, int limit, int window);

        /// <summary>
        /// Registers in-memory rate limiting services with policy-based sliding window configuration.
        /// Endpoints can specify policies using [RateLimit("policyName")] attribute from API project.
        /// Rate limiting is always enabled for security.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="serviceName">The service name for telemetry (must match OpenTelemetry service name).</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddInMemoryRateLimiting(this WebApplicationBuilder builder, string serviceName)
        {
            // Load and validate rate limit settings
            var settings = builder.Configuration.GetSection(RateLimitSettings.ConfigurationSectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            // Register settings and services as singletons for DI
            builder.Services.AddSingleton<ClientIpResolver>();
            builder.Services.AddSingleton(new RateLimitMetrics(serviceName));

            // Register rate limiting with partitioned policies
            builder.Services.AddRateLimiter(options =>
            {
                // Global limiter partitions requests by IP address and endpoint policy
                // Note: Rate limiting occurs BEFORE authentication, so we cannot distinguish
                // authenticated vs anonymous users. All requests get the same limit per policy.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // Exempt configured paths from rate limiting
                    if (IsExemptPath(context, settings))
                    {
                        // Store partition info for response headers
                        context.Items["RateLimit:Policy"] = "exempt";
                        context.Items["RateLimit:Limit"] = "unlimited";
                        context.Items["RateLimit:Window"] = "N/A";
                        return RateLimitPartition.GetNoLimiter("exempt");
                    }

                    // Determine which policy to apply based on endpoint attribute
                    var policyName = GetPolicyNameFromEndpoint(context, settings);

                    // Apply IP-based rate limiting with the selected policy
                    return CreateIpBasedPartition(context, settings, policyName);
                });

                // Configure rejection handler with logging and metrics
                options.OnRejected = (context, cancellationToken) =>
                {
                    HandleRateLimitRejection(context, settings);
                    return new ValueTask();
                };
            });

            return builder;
        }

        /// <summary>
        /// Applies rate limiting middleware to the request pipeline.
        /// Must be called BEFORE UseAuthentication() to protect auth endpoints.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        /// <remarks>
        /// IMPORTANT: If deploying behind a reverse proxy (nginx, HAProxy, load balancer, CDN),
        /// you must call app.UseForwardedHeaders() BEFORE this method to ensure X-Forwarded-For
        /// headers are processed correctly. Without it, all requests from the same proxy will
        /// share one rate limit. See Program.cs for configuration example.
        /// </remarks>
        public static WebApplication UseInMemoryRateLimiting(this WebApplication app)
        {
            // Apply ASP.NET Core's rate limiting middleware
            app.UseRateLimiter();

            // Add RFC-compliant rate limit headers to all responses (for client visibility)
            app.UseMiddleware<RateLimitHeadersMiddleware>();

            return app;
        }

        /// <summary>
        /// Checks if the request path matches any of the configured exempt paths.
        /// Exempt paths bypass rate limiting (e.g., health checks, metrics).
        /// </summary>
        private static bool IsExemptPath(HttpContext context, RateLimitSettings settings)
        {
            var path = context.Request.Path.ToString();
            return settings.ExemptPaths?.Any(exemptPath => path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        /// <summary>
        /// Determines which rate limit policy to apply based on endpoint metadata.
        /// Looks for [RateLimit("policyName")] attribute on action or controller.
        /// Falls back to "default" policy if no attribute is present.
        /// </summary>
        private static string GetPolicyNameFromEndpoint(HttpContext context, RateLimitSettings settings)
        {
            // Check if endpoint has RateLimit attribute
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                // Get RateLimitAttribute directly (no reflection needed)
                var rateLimitAttribute = endpoint.Metadata
                    .GetMetadata<RateLimitAttribute>();

                if (rateLimitAttribute != null)
                {
                    var policyName = rateLimitAttribute.PolicyName;

                    if (!string.IsNullOrWhiteSpace(policyName))
                    {
                        // Validate that the policy exists in configuration
                        if (settings.Policies.ContainsKey(policyName))
                        {
                            return policyName;
                        }

                        // Log warning if policy not found (misconfiguration)
                        var logger = context.RequestServices.GetRequiredService<ILogger<WebApplication>>();

                        LogPolicyNotFound(logger, policyName, RateLimitPolicies.Default, context.Request.Method, context.Request.Path);
                    }
                }
            }

            // Use default policy if no attribute or policy not found
            return RateLimitPolicies.Default;
        }

        /// <summary>
        /// Creates a rate limit partition for requests based on IP address and policy.
        /// Since rate limiting occurs before authentication, we cannot distinguish between
        /// authenticated and anonymous users, so all requests use the same limit per policy.
        /// </summary>
        private static RateLimitPartition<string> CreateIpBasedPartition(
            HttpContext context,
            RateLimitSettings settings,
            string policyName)
        {
            var ipResolver = context.RequestServices.GetRequiredService<ClientIpResolver>();
            var ipAddress = ipResolver.GetClientIpAddress(context);
            var policy = settings.Policies[policyName];

            // Store partition info for response headers
            context.Items["RateLimit:Policy"] = policyName;
            context.Items["RateLimit:Limit"] = policy.PermitLimit;
            context.Items["RateLimit:Window"] = policy.WindowSeconds;

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"{policyName}-ip-{ipAddress}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                    SegmentsPerWindow = settings.SegmentsPerWindow,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        /// <summary>
        /// Handles rate limit rejection by logging the event, recording metrics, and throwing an exception.
        /// Exception is caught by ExceptionHandlingMiddleware which returns 429 status code.
        /// </summary>
        private static void HandleRateLimitRejection(
            OnRejectedContext context,
            RateLimitSettings settings)
        {
            // Resolve services from request scope
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<WebApplication>>();
            var metrics = context.HttpContext.RequestServices.GetRequiredService<RateLimitMetrics>();
            var ipResolver = context.HttpContext.RequestServices.GetRequiredService<ClientIpResolver>();

            var endpoint = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            var ipAddress = ipResolver.GetClientIpAddress(context.HttpContext);

            // Get policy name from context (set during partition creation)
            var policyName = context.HttpContext.Items["RateLimit:Policy"]?.ToString() ?? "unknown";
            if (!settings.Policies.TryGetValue(policyName, out var policy))
            {
                policy = settings.Policies[RateLimitPolicies.Default];
            }

            // Calculate retry and reset times
            var retryAfterSeconds = policy.WindowSeconds;
            var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds();

            // Add rate limiting headers before throwing exception
            // These will be preserved when ExceptionHandlingMiddleware catches the exception

            // RFC-compliant IETF Draft headers
            context.HttpContext.Response.Headers[Headers.RateLimitLimit] = policy.PermitLimit.ToString(CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers[Headers.RateLimitReset] = resetTimestamp.ToString(CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers[Headers.RetryAfter] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            // Custom headers for additional context (backward compatibility)
            context.HttpContext.Response.Headers[Headers.RateLimitPolicy] = policyName;
            context.HttpContext.Response.Headers[Headers.RateLimitWindow] = policy.WindowSeconds.ToString(CultureInfo.InvariantCulture);

            // Log rate limit rejection
            LogRateLimitExceeded(
                logger, ipAddress, policyName, method, endpoint,
                policy.PermitLimit,
                policy.WindowSeconds);

            metrics.RecordRejection(policyName, method);

            // Throw custom exception for ExceptionHandlingMiddleware
            // Headers are already set above, exception carries context for response body
            throw new RateLimitExceededException(
                policyName,
                policy.PermitLimit,
                policy.WindowSeconds,
                retryAfterSeconds,
                resetTimestamp);
        }
    }
}
