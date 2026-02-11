using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using emc.camus.security.apikey.Handlers;
using emc.camus.security.apikey.Configurations;
using emc.camus.application.Auth;
using emc.camus.application.Secrets;
using System.Diagnostics.CodeAnalysis;

namespace emc.camus.security.apikey
{
    /// <summary>
    /// Provides extension methods for configuring API Key authentication.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApiKeySetupExtensions
    {
        /// <summary>
        /// Adds API Key authentication services.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// This method requires an <see cref="ISecretProvider"/> to be registered in the service collection
        /// before calling this method. The secret provider must provide the API key as configured
        /// in ApiKeySettings.SecretKeyName (defaults to "XApiKey").
        /// </remarks>
        public static WebApplicationBuilder AddApiKeyAuthentication(this WebApplicationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("ApiKeySecuritySetup");
            
            logger.LogInformation("Configuring API Key authentication services");

            // Load, validate, and register API Key Settings
            var settings = builder.Configuration.GetSection(ApiKeySettings.ConfigurationSectionName).Get<ApiKeySettings>() ?? new ApiKeySettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    AuthenticationSchemes.ApiKey,
                    null);

            logger.LogInformation("API Key authentication configured with scheme: {Scheme}", 
                AuthenticationSchemes.ApiKey);

            return builder;
        }
    }
}
