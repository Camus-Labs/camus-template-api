using System.Diagnostics.CodeAnalysis;
using emc.camus.application.ApiInfo;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory.Repositories;

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
        /// Loads AppDataSettings from configuration and validates settings.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddAppData(this WebApplicationBuilder builder)
        {
            // Load and validate application data settings
            var settings = builder.Configuration
                .GetSection(AppDataSettings.ConfigurationSectionName)
                .Get<AppDataSettings>() ?? new AppDataSettings();
            
            settings.Validate();
            
            // Register settings as singleton
            builder.Services.AddSingleton(settings);

            // Register API info repository based on provider
            if (settings.Provider == AppDataProvider.InMemory)
            {
                builder.Services.AddSingleton<IApiInfoRepository, InMemoryApiInfoRepository>();
            }
            else if (settings.Provider == AppDataProvider.Database)
            {
                // TODO: Implement database repository
                throw new NotImplementedException("Database application data provider is not yet implemented. Use 'InMemory' provider.");
            }

            // Register application service for API info
            builder.Services.AddScoped<ApiInfoService>();

            return builder;
        }

        /// <summary>
        /// Initializes the API info repository to load API data.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseAppDataSetup(this WebApplication app)
        {
            // Initialize API info repository to load API data
            var apiInfoRepository = app.Services.GetRequiredService<IApiInfoRepository>();
            apiInfoRepository.Initialize();
            
            return app;
        }
    }
}
