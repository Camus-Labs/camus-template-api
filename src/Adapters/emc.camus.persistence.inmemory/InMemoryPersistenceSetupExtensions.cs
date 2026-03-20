using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.Common;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.Services;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

namespace emc.camus.persistence.inmemory
{
    /// <summary>
    /// Provides extension methods for configuring in-memory persistence services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class InMemoryPersistenceSetupExtensions
    {
        /// <summary>
        /// Adds in-memory persistence services including unit of work and all repositories.
        /// Loads and validates <see cref="InMemoryModelSettings"/> from configuration.
        /// Useful for development, testing, and scenarios where database is not required.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        /// <remarks>
        /// Data is stored in memory and will be lost when the application restarts.
        /// </remarks>
        public static WebApplicationBuilder AddInMemoryPersistence(this WebApplicationBuilder builder)
        {
            // Load and validate in-memory model settings
            var inMemorySettings = builder.Configuration
                .GetSection(InMemoryModelSettings.ConfigurationSectionName)
                .Get<InMemoryModelSettings>() ?? new InMemoryModelSettings();

            inMemorySettings.Validate();

            builder.Services.AddSingleton(inMemorySettings);

            // Register unit of work (scoped: one per request, no-op for in-memory)
            builder.Services.AddScoped<IUnitOfWork, IMUnitOfWork>();

            // Register audit repository (shared across Auth and AppData)
            builder.Services.AddSingleton<IActionAuditRepository, IMActionAuditRepository>();

            // Register repositories as singletons (to persist data during app lifetime)
            builder.Services.AddSingleton<IUserRepository, IMUserRepository>();
            builder.Services.AddSingleton<IApiInfoRepository, IMApiInfoRepository>();

            return builder;
        }
    }
}
