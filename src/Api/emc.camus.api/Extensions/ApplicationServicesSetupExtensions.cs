using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Infrastructure;
using emc.camus.api.Middleware;
using emc.camus.application.Auth;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring ASP.NET Core controllers and application services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApplicationServicesSetupExtensions
    {
        /// <summary>
        /// Registers ASP.NET Core MVC controllers and core application services.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
        {
            // Register HTTP Context Accessor (required for accessing current user in services)
            builder.Services.AddHttpContextAccessor();

            // Register User Context (provides access to current authenticated user)
            builder.Services.AddScoped<IUserContext, HttpUserContext>();

            // Register Authentication Service (business logic in Application layer)
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IApiInfoService, ApiInfoService>();

            // Add controllers
            builder.Services.AddControllers();

            return builder;
        }

        /// <summary>
        /// Configures application-level middleware and endpoint routing in the request pipeline.
        /// Adds Username header middleware and maps controller endpoints.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        /// <remarks>
        /// This method adds Username header to all responses (authenticated users get their username,
        /// anonymous requests get "anonymous") for observability and debugging purposes.
        /// Must be called after UseAuthentication() in the pipeline.
        /// </remarks>
        public static WebApplication UseApplicationServices(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            // Add Username header to responses (for observability and debugging)
            app.UseMiddleware<UsernameHeaderMiddleware>();

            // Map controller endpoints
            app.MapControllers();

            return app;
        }
    }
}
