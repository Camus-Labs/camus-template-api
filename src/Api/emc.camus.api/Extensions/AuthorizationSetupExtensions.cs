using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.persistence.inmemory;
using emc.camus.persistence.postgresql;

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
        public static WebApplicationBuilder AddAuthorizationWithData(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Configure Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                // Register a policy for each permission that validates the permission claim exists in the token.
                // These policies are used by [RequirePermission(Permissions.XYZ)] attribute on controller endpoints.
                foreach (var permission in Permissions.GetAll())
                {
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim(Permissions.ClaimType, permission));
                }
            });

            // Load and validate authorization settings
            var settings = builder.Configuration
                .GetSection(AuthorizationSettings.ConfigurationSectionName)
                .Get<AuthorizationSettings>() ?? new AuthorizationSettings();
            
            settings.Validate();
            
            // Register settings as singleton
            builder.Services.AddSingleton(settings);

            // Delegate to appropriate persistence adapter
            if (settings.Provider == AuthorizationProvider.InMemory)
            {
                builder.AddInMemoryPersistence(PersistenceFeatures.Auth);
            }
            else if (settings.Provider == AuthorizationProvider.Database)
            {
                builder.AddPostgreSqlPersistence(PersistenceFeatures.Auth);
            }

            return builder;
        }

        /// <summary>
        /// Initializes the auth service to load users and roles.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseAuthorizationWithData(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            // Initialize auth service to load users/roles and validate secrets
            // AuthService is scoped, so we need to create a scope to resolve it
            using (var scope = app.Services.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
                authService.Initialize();
            }
            
            app.UseAuthorization();
            
            return app;
        }
    }
}
