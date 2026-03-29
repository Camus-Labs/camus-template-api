using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Middleware;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring security headers middleware.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SecurityHeadersSetupExtensions
    {
        /// <summary>
        /// Registers the security headers middleware in the request pipeline.
        /// Adds X-Content-Type-Options, X-Frame-Options, Referrer-Policy, and Content-Security-Policy
        /// headers to all responses.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>();

            return app;
        }
    }
}
