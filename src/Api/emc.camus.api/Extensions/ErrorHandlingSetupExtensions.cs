using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Configurations;
using emc.camus.api.Middleware;
using emc.camus.api.Metrics;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring error handling services and middleware.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ErrorHandlingSetupExtensions
    {
        /// <summary>
        /// Registers error handling services including error code resolution and metrics.
        /// Loads ErrorHandlingSettings from configuration and validates rules.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="serviceName">The service name for metrics instrumentation.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder, string serviceName)
        {
            // Load and validate error handling settings
            var settings = builder.Configuration
                .GetSection(ErrorHandlingSettings.ConfigurationSectionName)
                .Get<ErrorHandlingSettings>() ?? new ErrorHandlingSettings();
            
            settings.Validate();
            
            // Register settings as singleton
            builder.Services.AddSingleton(settings);
            
            // Register error metrics
            builder.Services.AddSingleton(sp => 
                new ErrorMetrics(serviceName, sp.GetRequiredService<ILogger<ErrorMetrics>>()));
            
            return builder;
        }

        /// <summary>
        /// Registers the exception handling middleware in the request pipeline.
        /// Should be called early in the pipeline to catch all downstream exceptions.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseErrorHandling(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}
