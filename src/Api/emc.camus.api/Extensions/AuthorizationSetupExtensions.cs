using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory.Repositories;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring authorization services and user repositories.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AuthorizationSetupExtensions
    {
        /// <summary>
        /// Registers authorization services including policies and user repository.
        /// Loads AuthorizationSettings from configuration and validates settings.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddAuthorization(this WebApplicationBuilder builder)
        {
            // Configure Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                // Add custom authorization policies here as needed
                // Example: options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            });

            // Load and validate authorization settings
            var settings = builder.Configuration
                .GetSection(AuthorizationSettings.ConfigurationSectionName)
                .Get<AuthorizationSettings>() ?? new AuthorizationSettings();
            
            settings.Validate();
            
            // Register settings as singleton
            builder.Services.AddSingleton(settings);

            // Register user repository based on authorization provider
            if (settings.Provider == AuthorizationProvider.InMemory)
            {
                builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            }
            else if (settings.Provider == AuthorizationProvider.Database)
            {
                // TODO: Implement database repository
                throw new NotImplementedException("Database authorization provider is not yet implemented. Use 'InMemory' provider.");
            }

            return builder;
        }

        /// <summary>
        /// Initializes the user repository to load users and roles.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseAuthorizationSetup(this WebApplication app)
        {
            app.UseAuthorization();
            // Initialize user repository to load users/roles and validate secrets
            var userRepository = app.Services.GetRequiredService<IUserRepository>();
            userRepository.Initialize();
            
            return app;
        }
    }
}
