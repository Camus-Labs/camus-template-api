using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Configurations;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Cross-Origin Resource Sharing (CORS) policies.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class CorsSetupExtensions
    {
        /// <summary>
        /// Registers CORS services with configured policies.
        /// Loads CorsSettings from configuration and validates settings.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
        {
            // Load and validate CORS settings
            var settings = builder.Configuration
                .GetSection(CorsSettings.ConfigurationSectionName)
                .Get<CorsSettings>() ?? new CorsSettings();

            settings.Validate();

            // Register settings as singleton
            builder.Services.AddSingleton(settings);

            // Configure CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(settings.PolicyName, policy =>
                {
                    policy.WithOrigins(settings.AllowedOrigins)
                          .WithMethods(settings.AllowedMethods)
                          .WithHeaders(settings.AllowedHeaders)
                          .WithExposedHeaders(settings.ExposedHeaders);

                    if (settings.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }

                    policy.SetPreflightMaxAge(TimeSpan.FromMinutes(settings.PreflightMaxAgeMinutes));
                });
            });

            return builder;
        }

        /// <summary>
        /// Applies the configured CORS policy to the request pipeline.
        /// Ensures all responses include proper CORS headers.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseCorsPolicy(this WebApplication app)
        {
            // Load CORS settings from DI and apply policy
            var settings = app.Services.GetRequiredService<CorsSettings>();
            app.UseCors(settings.PolicyName);

            return app;
        }
    }
}
