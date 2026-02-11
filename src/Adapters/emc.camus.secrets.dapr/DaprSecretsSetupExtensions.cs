using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using emc.camus.application.Secrets;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;
using System.Diagnostics.CodeAnalysis;

namespace emc.camus.secrets.dapr
{
    /// <summary>
    /// Provides extension methods for configuring Dapr secrets services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DaprSecretsSetupExtensions
    {
        /// <summary>
        /// Adds Dapr secret provider services.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// This method configures the Dapr secret provider to communicate with a Dapr sidecar.
        /// Configuration is read from the "DaprSecretProvider" section in appsettings.json.
        /// Secrets are loaded during construction and cached in memory.
        /// </remarks>
        public static WebApplicationBuilder AddDaprSecrets(this WebApplicationBuilder builder)
        {
            // Load, validate, and register Dapr Secret Provider Settings
            var settings = builder.Configuration.GetSection(DaprSecretProviderSettings.ConfigurationSectionName).Get<DaprSecretProviderSettings>() ?? new DaprSecretProviderSettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);
            
            // Register DaprSecretProvider with HttpClient
            builder.Services.AddHttpClient<DaprSecretProvider>();
            
            // Register as ISecretProvider singleton
            builder.Services.AddSingleton<ISecretProvider>(provider => 
                provider.GetRequiredService<DaprSecretProvider>());

            return builder;
        }

        /// <summary>
        /// Initializes the Dapr secret provider at application startup.
        /// This forces the singleton to be created and all secrets to be loaded eagerly,
        /// ensuring the application fails fast if there are any issues connecting to Dapr
        /// or loading secrets rather than failing on first usage.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseDaprSecrets(this WebApplication app)
        {
            // Force ISecretProvider singleton creation during startup
            var secretProvider = app.Services.GetRequiredService<ISecretProvider>();
            
            // Load all configured secrets and fail fast if there are any issues
            var settings = app.Services.GetRequiredService<DaprSecretProviderSettings>();
            secretProvider.LoadSecretsAsync(settings.SecretNames).GetAwaiter().GetResult();
            
            return app;
        }
    }
}
