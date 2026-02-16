using System.Diagnostics.CodeAnalysis;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.postgresql.Data;
using emc.camus.persistence.postgresql.Repositories;

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
                // Register database settings with key
                builder.Services.AddKeyedSingleton(ConnectionFactoryKeys.AppData, settings.Database);
                
                // Register database connection factory with key for AppData
                builder.Services.AddKeyedSingleton(ConnectionFactoryKeys.AppData, (sp, key) =>
                {
                    var dbSettings = sp.GetRequiredKeyedService<DatabaseSettings>(key);
                    var logger = sp.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
                    return new NpgsqlConnectionFactory(dbSettings, logger);
                });
                
                builder.Services.AddScoped<IApiInfoRepository, PostgreSqlApiInfoRepository>();
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
