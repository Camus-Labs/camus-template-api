using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring ASP.NET Core controllers and application services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApplicationServicesSetupExtensions
    {
        /// <summary>
        /// Registers ASP.NET Core MVC controllers.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
        {
            // Register Authentication Service (business logic in Application layer)
            builder.Services.AddScoped<AuthService>();
            
            // Add controllers
            builder.Services.AddControllers();

            return builder;
        }

        /// <summary>
        /// Configures application-level middleware and endpoint routing in the request pipeline.
        /// Maps controller endpoints.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseApplicationServices(this WebApplication app)
        {
            // Map controller endpoints
            app.MapControllers();
            
            return app;
        }
    }
}