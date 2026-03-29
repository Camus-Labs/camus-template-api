using System.Diagnostics.CodeAnalysis;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory;
using emc.camus.persistence.postgresql;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring data persistence services.
    /// Reads <see cref="DataPersistenceSettings"/> from configuration and delegates
    /// to the appropriate persistence adapter (in-memory or PostgreSQL).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class PersistenceSetupExtensions
    {
        /// <summary>
        /// Registers data persistence services based on configuration.
        /// Loads <see cref="DataPersistenceSettings"/> to determine the provider, then delegates
        /// to the appropriate persistence adapter which loads its own provider-specific settings.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
        {
            var persistenceSettings = builder.Configuration
                .GetSection(DataPersistenceSettings.ConfigurationSectionName)
                .Get<DataPersistenceSettings>() ?? new DataPersistenceSettings();

            persistenceSettings.Validate();

            builder.Services.AddSingleton(persistenceSettings);

            if (persistenceSettings.Provider == PersistenceProvider.InMemory)
            {
                builder.AddInMemoryPersistence();
            }
            else if (persistenceSettings.Provider == PersistenceProvider.PostgreSQL)
            {
                var dbSettings = builder.Configuration
                    .GetSection(DatabaseSettings.ConfigurationSectionName)
                    .Get<DatabaseSettings>() ?? new DatabaseSettings();

                dbSettings.Validate();

                builder.Services.AddSingleton(dbSettings);
                builder.AddPostgreSqlPersistence();
            }

            return builder;
        }

        /// <summary>
        /// Initializes persistence-dependent services such as API info data loading.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UsePersistence(this WebApplication app)
        {
            // Initialize API info and Auth service to validate required data
            // Services are scoped, so we need to create a scope to resolve them
            using (var scope = app.Services.CreateScope())
            {
                var apiInfoService = scope.ServiceProvider.GetRequiredService<IApiInfoService>();
                apiInfoService.Initialize();

                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                authService.Initialize();
            }

            return app;
        }
    }
}
