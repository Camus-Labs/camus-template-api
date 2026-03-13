using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory;
using emc.camus.persistence.postgresql;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring application data services and repositories.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AppDataSetupExtensions
    {
        /// <summary>
        /// Registers application data services including API info repository.
        /// Loads AppDataSettings from configuration and delegates to appropriate persistence adapter.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddAppData(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Load and validate application data settings
            var settings = builder.Configuration
                .GetSection(AppDataSettings.ConfigurationSectionName)
                .Get<AppDataSettings>() ?? new AppDataSettings();
            
            settings.Validate();
            
            // Register settings as singleton
            builder.Services.AddSingleton(settings);

            // Delegate to appropriate persistence adapter
            if (settings.Provider == AppDataProvider.InMemory)
            {
                builder.AddInMemoryPersistence(PersistenceFeatures.AppData);
            }
            else if (settings.Provider == AppDataProvider.Database)
            {
                builder.AddPostgreSqlPersistence(PersistenceFeatures.AppData);
            }

            // Register application service for API info
            builder.Services.AddScoped<ApiInfoService>();

            return builder;
        }

        /// <summary>
        /// Initializes the API info service to load API data.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseAppData(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            // Initialize API info service to load API data
            // ApiInfoService is scoped, so we need to create a scope to resolve it
            using (var scope = app.Services.CreateScope())
            {
                var apiInfoService = scope.ServiceProvider.GetRequiredService<ApiInfoService>();
                apiInfoService.Initialize();
            }
            
            return app;
        }
    }
}
