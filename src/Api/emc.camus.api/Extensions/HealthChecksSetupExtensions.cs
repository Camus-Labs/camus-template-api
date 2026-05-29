using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text.Json;
using emc.camus.api.Models.Responses;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.persistence.postgresql;
using emc.camus.secrets.dapr;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring health check services and endpoints.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class HealthChecksSetupExtensions
    {
        /// <summary>
        /// Registers health check services. Custom health checks (e.g., database connectivity)
        /// can be added by chaining <c>.AddCheck&lt;T&gt;()</c> on the returned builder.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
        {
            var healthChecksBuilder = builder.Services.AddHealthChecks();

            // Dapr secret store health check (verifies secret store accessibility)
            healthChecksBuilder.AddDaprSecretHealthCheck();

            // PostgreSQL health check (only when using PostgreSQL provider)
            var persistenceSettings = builder.Configuration
                .GetSection(DataPersistenceSettings.ConfigurationSectionName)
                .Get<DataPersistenceSettings>() ?? new DataPersistenceSettings();

            if (persistenceSettings.Provider == PersistenceProvider.PostgreSQL)
            {
                healthChecksBuilder.AddPostgreSqlHealthCheck();
            }

            return builder;
        }

        /// <summary>
        /// Maps health check endpoints for container orchestration and load balancer probes.
        /// All endpoints allow anonymous access and are exempt from rate limiting.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        /// <remarks>
        /// Three endpoints are exposed:
        /// <list type="bullet">
        /// <item><c>/health</c> — overall health (runs all registered checks)</item>
        /// <item><c>/alive</c> — liveness probe (always returns 200 if process is running)</item>
        /// <item><c>/ready</c> — readiness probe (runs checks tagged with HealthCheckTags.Ready)</item>
        /// </list>
        /// </remarks>
        public static WebApplication UseHealthChecks(this WebApplication app)
        {
            // Overall health — runs all registered checks, returns per-check JSON details
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteDetailedJsonResponse
            }).AllowAnonymous();

            // Liveness probe — always healthy if the process is running
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = _ => false
            }).AllowAnonymous();

            // Readiness probe — only checks tagged with HealthCheckTags.Ready
            app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
            }).AllowAnonymous();

            return app;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Writes a JSON response containing the overall status and per-check details.
        /// </summary>
        private static async Task WriteDetailedJsonResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var response = new HealthCheckDetailedResponse
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.Select(entry => new HealthCheckEntryResponse
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration.TotalMilliseconds
                })
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, JsonOptions),
                context.RequestAborted);
        }
    }
}
