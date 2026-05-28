using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using emc.camus.api.Configurations;
using emc.camus.api.Infrastructure;
using emc.camus.api.Metrics;
using emc.camus.api.Middleware;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.application.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.Threading.RateLimiting;

namespace emc.camus.api.Extensions;

/// <summary>
/// Provides extension methods for configuring rate limiting in the API pipeline.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "DI wiring with framework-coupled branching logic (HttpContext partitioning, endpoint metadata) impractical to unit-test in isolation")]
public static class RateLimitingSetupExtensions
{
    /// <summary>
    /// Registers rate limiting services with policy-based sliding window configuration.
    /// Wires ASP.NET Core PartitionedRateLimiter with ConcurrentDictionary-based partition storage.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">The service name for telemetry.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        // Load and validate rate limit settings
        var settings = builder.Configuration.GetSection(RateLimitingSettings.ConfigurationSectionName).Get<RateLimitingSettings>() ?? new RateLimitingSettings();
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
                    context.Items[RateLimitContextKeys.Policy] = "exempt";
                    context.Items[RateLimitContextKeys.Limit] = "unlimited";
                    context.Items[RateLimitContextKeys.Window] = "N/A";
                    return RateLimitPartition.GetNoLimiter("exempt");
                }

                // Determine which policy to apply based on endpoint attribute
                var policyName = GetPolicyNameFromEndpoint(context, settings);

                // Apply IP-based rate limiting with the selected policy
                return CreateIpBasedPartition(context, settings, policyName);
            });

            // Configure rejection handler with logging and metrics
            options.OnRejected = (context, _) =>
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
    public static WebApplication UseRateLimiting(this WebApplication app)
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
    private static bool IsExemptPath(HttpContext context, RateLimitingSettings settings)
    {
        var path = context.Request.Path.ToString();
        return settings.ExemptPaths?.Any(exemptPath => path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    /// <summary>
    /// Determines which rate limit policy to apply based on endpoint metadata.
    /// Looks for [RateLimit("policyName")] attribute on action or controller.
    /// Falls back to "default" policy if no attribute is present.
    /// </summary>
    private static string GetPolicyNameFromEndpoint(HttpContext context, RateLimitingSettings settings)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var rateLimitAttribute = endpoint.Metadata.GetMetadata<RateLimitAttribute>();

            if (rateLimitAttribute != null)
            {
                var policyName = rateLimitAttribute.PolicyName;

                if (!string.IsNullOrWhiteSpace(policyName))
                {
                    if (settings.Policies.ContainsKey(policyName))
                    {
                        return policyName;
                    }

                    var availablePolicies = string.Join(", ", settings.Policies.Keys);
                    throw new InvalidOperationException(
                        $"Rate limit policy '{policyName}' referenced by endpoint [{context.Request.Method}] {context.Request.Path} " +
                        $"is not defined in configuration. Available policies: {availablePolicies}");
                }
            }
        }

        return RateLimitPolicies.Default;
    }

    /// <summary>
    /// Creates a rate limit partition for requests based on IP address and policy.
    /// Since rate limiting occurs before authentication, we cannot distinguish between
    /// authenticated and anonymous users, so all requests use the same limit per policy.
    /// </summary>
    private static RateLimitPartition<string> CreateIpBasedPartition(
        HttpContext context,
        RateLimitingSettings settings,
        string policyName)
    {
        var ipResolver = context.RequestServices.GetRequiredService<ClientIpResolver>();
        var ipAddress = ipResolver.GetClientIpAddress(context);
        var policy = settings.Policies[policyName];

        // Store partition info for response headers
        context.Items[RateLimitContextKeys.Policy] = policyName;
        context.Items[RateLimitContextKeys.Limit] = policy.PermitLimit;
        context.Items[RateLimitContextKeys.Window] = policy.WindowSeconds;

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
        RateLimitingSettings settings)
    {
        // Resolve services from request scope
        var metrics = context.HttpContext.RequestServices.GetRequiredService<RateLimitMetrics>();
        var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();

        var method = context.HttpContext.Request.Method;

        // Get policy name from context (set during partition creation)
        var policyName = context.HttpContext.Items[RateLimitContextKeys.Policy]?.ToString() ?? "unknown";
        if (!settings.Policies.TryGetValue(policyName, out var policy))
        {
            policy = settings.Policies[RateLimitPolicies.Default];
        }

        // Calculate retry and reset times
        var retryAfterSeconds = policy.WindowSeconds;
        var resetTimestamp = timeProvider.GetUtcNow().AddSeconds(retryAfterSeconds).ToUnixTimeSeconds();

        // Add rate limiting headers before throwing exception
        // These will be preserved when ExceptionHandlingMiddleware catches the exception

        // RFC-compliant IETF Draft headers
        context.HttpContext.Response.Headers[Headers.RateLimitLimit] = policy.PermitLimit.ToString(CultureInfo.InvariantCulture);
        context.HttpContext.Response.Headers[Headers.RateLimitReset] = resetTimestamp.ToString(CultureInfo.InvariantCulture);
        context.HttpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        // Custom headers for additional context (backward compatibility)
        context.HttpContext.Response.Headers[Headers.RateLimitPolicy] = policyName;
        context.HttpContext.Response.Headers[Headers.RateLimitWindow] = policy.WindowSeconds.ToString(CultureInfo.InvariantCulture);

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
