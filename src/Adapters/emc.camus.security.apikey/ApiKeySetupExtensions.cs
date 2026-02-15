using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.security.apikey.Handlers;
using emc.camus.security.apikey.Configurations;
using emc.camus.application.Auth;
using emc.camus.application.Common;
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
        /// before calling this method. The secret provider must provide the API key using the secret name
        /// from the secret name configured in <see cref="ApiKeySettings.ApiKeySecretName"/>.
        /// </remarks>
        public static WebApplicationBuilder AddApiKeyAuthentication(this WebApplicationBuilder builder)
        {
            // Load, validate, and register API Key Settings
            var settings = builder.Configuration.GetSection(ApiKeySettings.ConfigurationSectionName).Get<ApiKeySettings>() ?? new ApiKeySettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    AuthenticationSchemes.ApiKey,
                    null);

            return builder;
        }
    }
}
