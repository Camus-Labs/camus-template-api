using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.Common;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.inmemory
{
    /// <summary>
    /// Provides extension methods for configuring in-memory persistence services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class InMemoryPersistenceSetupExtensions
    {
        /// <summary>
        /// Adds in-memory persistence services for selected repositories.
        /// Useful for development, testing, and scenarios where database is not required.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="features">Features to register. Use flags to combine multiple features.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <remarks>
        /// Data is stored in memory and will be lost when the application restarts.
        /// </remarks>
        public static IServiceCollection AddInMemoryPersistence(
            this IServiceCollection services,
            PersistenceFeatures features = PersistenceFeatures.All)
        {
            // Register audit repository (last call wins, shared across Auth and AppData)
            services.Replace(ServiceDescriptor.Singleton<IActionAuditRepository, IMActionAuditRepository>());

            // Register repositories as singletons (to persist data during app lifetime)
            if (features.HasFlag(PersistenceFeatures.Auth))
            {
                services.Replace(ServiceDescriptor.Singleton<IUserRepository, IMUserRepository>());
            }
            
            if (features.HasFlag(PersistenceFeatures.AppData))
            {
                services.Replace(ServiceDescriptor.Singleton<IApiInfoRepository, IMApiInfoRepository>());
            }

            return services;
        }
    }
}
