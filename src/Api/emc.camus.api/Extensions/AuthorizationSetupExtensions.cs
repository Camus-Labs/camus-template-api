using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.application.Auth;
using emc.camus.application.Common;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring authorization services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AuthorizationSetupExtensions
    {
        /// <summary>
        /// Registers authorization policies based on application permissions.
        /// Persistence is configured separately via <see cref="PersistenceSetupExtensions.AddPersistence"/>.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddAuthorizationPolicies(this WebApplicationBuilder builder)
        {
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

            return builder;
        }

        /// <summary>
        /// Initializes the auth service to load users and roles.
        /// Should be called during application startup after services are built.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseAuthorizationPolicies(this WebApplication app)
        {
            app.UseAuthorization();

            return app;
        }
    }
}
