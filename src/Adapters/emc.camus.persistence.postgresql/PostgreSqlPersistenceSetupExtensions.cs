using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.persistence.postgresql.Data;
using emc.camus.persistence.postgresql.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.postgresql
{
    /// <summary>
    /// Provides extension methods for configuring PostgreSQL persistence services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class PostgreSqlPersistenceSetupExtensions
    {
        /// <summary>
        /// Adds PostgreSQL persistence services including connection factory and selected repositories.
        /// Loads DatabaseSettings from configuration and registers it as singleton if not already registered.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="features">Features to register. Use flags to combine multiple features.</param>
        /// <returns>The web application builder for method chaining.</returns>
        /// <remarks>
        /// Requires IUserContext to be registered before calling this method (mandatory for audit trail functionality).
        /// Automatically sets session variables (app.current_username) for database triggers.
        /// Will throw exception during service resolution if IUserContext is not registered.
        /// </remarks>
        public static WebApplicationBuilder AddPostgreSqlPersistence(
            this WebApplicationBuilder builder,
            PersistenceFeatures features = PersistenceFeatures.All)
        {
            // Load DatabaseSettings from root configuration
            var settings = builder.Configuration.GetSection(DatabaseSettings.ConfigurationSectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();
            settings.Validate();
            
            // Register DatabaseSettings as singleton (only if not already registered)
            builder.Services.TryAddSingleton(settings);
            
            // Register database connection factory
            builder.Services.TryAddSingleton<IConnectionFactory, NpgsqlConnectionFactory>();

            // Register audit repository (shared across Auth and AppData)
            builder.Services.TryAddScoped<IActionAuditRepository, PSActionAuditRepository>();

            // Register repositories based on features
            if (features.HasFlag(PersistenceFeatures.Auth))
            {
                builder.Services.TryAddScoped<IUserRepository, PSUserRepository>();
            }
            
            if (features.HasFlag(PersistenceFeatures.AppData))
            {
                builder.Services.TryAddScoped<IApiInfoRepository, PSApiInfoRepository>();
            }

            return builder;
        }
    }
}
