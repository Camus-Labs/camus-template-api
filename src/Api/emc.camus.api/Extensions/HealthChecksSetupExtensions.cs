using System.Diagnostics.CodeAnalysis;
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
        /// <item><c>/ready</c> — readiness probe (runs checks tagged with "ready")</item>
        /// </list>
        /// </remarks>
        public static WebApplication UseHealthChecks(this WebApplication app)
        {
            // Overall health — runs all registered checks
            app.MapHealthChecks("/health").AllowAnonymous();

            // Liveness probe — always healthy if the process is running
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = _ => false
            }).AllowAnonymous();

            // Readiness probe — only checks tagged with "ready"
            app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            }).AllowAnonymous();

            return app;
        }
    }
}
