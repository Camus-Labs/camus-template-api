using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Timeouts;
using emc.camus.api.Configurations;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring request timeout policies and middleware.
    /// Uses ASP.NET Core built-in request timeouts (Microsoft.AspNetCore.Http.Timeouts).
    /// Policy durations are configurable via RequestTimeoutSettings in appsettings.json.
    /// Endpoints opt in via [RequestTimeout] attribute with named policies or milliseconds.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class RequestTimeoutSetupExtensions
    {
        /// <summary>
        /// Registers request timeout services with named policies.
        /// Loads timeout durations from RequestTimeoutSettings configuration section.
        /// The default policy applies to all endpoints unless overridden.
        /// Endpoints can specify [RequestTimeout(RequestTimeoutPolicies.Tight)] or
        /// [RequestTimeout(RequestTimeoutPolicies.Extended)] for different limits.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddRequestTimeoutPolicies(this WebApplicationBuilder builder)
        {
            var settings = builder.Configuration
                .GetSection(RequestTimeoutSettings.ConfigurationSectionName)
                .Get<RequestTimeoutSettings>() ?? new RequestTimeoutSettings();

            settings.Validate();

            builder.Services.AddRequestTimeouts(options =>
            {
                options.DefaultPolicy = new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutSeconds)
                };

                options.AddPolicy(RequestTimeoutPolicies.Default, new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutSeconds)
                });

                options.AddPolicy(RequestTimeoutPolicies.Tight, new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(settings.TightTimeoutSeconds)
                });

                options.AddPolicy(RequestTimeoutPolicies.Extended, new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(settings.ExtendedTimeoutSeconds)
                });
            });

            return builder;
        }

        /// <summary>
        /// Registers the request timeout middleware in the request pipeline.
        /// Must be called AFTER UseRouting() and UseErrorHandling() so the framework
        /// can resolve endpoint metadata and timeout exceptions are caught.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseRequestTimeoutPolicies(this WebApplication app)
        {
            app.UseRequestTimeouts();
            return app;
        }
    }
}
