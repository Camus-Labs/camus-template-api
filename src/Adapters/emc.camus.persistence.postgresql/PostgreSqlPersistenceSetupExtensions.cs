using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
        /// Last registration wins - if called multiple times, the last settings will be used.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="settings">Database settings for PostgreSQL connection.</param>
        /// <param name="features">Features to register. Use flags to combine multiple features.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <remarks>
        /// Requires IUserContext to be registered before calling this method (mandatory for audit trail functionality).
        /// Automatically sets session variables (app.current_username) for database triggers.
        /// Will throw exception during service resolution if IUserContext is not registered.
        /// </remarks>
        public static IServiceCollection AddPostgreSqlPersistence(
            this IServiceCollection services,
            DatabaseSettings settings,
            PersistenceFeatures features = PersistenceFeatures.All)
        {
            // Register database settings (last call wins, replaces existing)
            services.Replace(ServiceDescriptor.Singleton(settings));
            
            // Register database connection factory (last call wins, replaces existing)
            services.Replace(ServiceDescriptor.Singleton<IConnectionFactory, NpgsqlConnectionFactory>());

            // Register audit repository (last call wins, shared across Auth and AppData)
            services.Replace(ServiceDescriptor.Scoped<IActionAuditRepository, PSActionAuditRepository>());

            // Register repositories based on features
            if (features.HasFlag(PersistenceFeatures.Auth))
            {
                services.AddScoped<IUserRepository, PSUserRepository>();
            }
            
            if (features.HasFlag(PersistenceFeatures.AppData))
            {
                services.AddScoped<IApiInfoRepository, PSApiInfoRepository>();
            }

            return services;
        }
    }
}
