using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpOverrides;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring transport security middleware
    /// (forwarded headers, HSTS, and HTTPS redirection).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class TransportSecuritySetupExtensions
    {
        /// <summary>
        /// Configures forwarded headers, HSTS, and HTTPS redirection in the request pipeline.
        /// Forwarded headers ensure correct client IP resolution behind proxies/load balancers.
        /// HSTS adds Strict-Transport-Security header in non-Development environments.
        /// HTTPS redirection enforces secure transport for all requests.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseTransportSecurity(this WebApplication app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardLimit = null
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            return app;
        }
    }
}
