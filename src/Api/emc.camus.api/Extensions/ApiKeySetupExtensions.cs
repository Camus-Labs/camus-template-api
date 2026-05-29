using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Configurations;
using emc.camus.api.Utilities;
using emc.camus.application.Auth;
using Microsoft.AspNetCore.Authentication;

namespace emc.camus.api.Extensions;

/// <summary>
/// Provides extension methods for configuring API Key authentication from the API layer.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApiKeyAuthSetupExtensions
{
    /// <summary>
    /// Adds API Key authentication services from the API layer.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for fluent configuration.</returns>
    /// <remarks>
    /// Requires an <see cref="emc.camus.application.Secrets.ISecretProvider"/> to be registered
    /// in the service collection before calling this method.
    /// </remarks>
    public static WebApplicationBuilder AddApiKeyAuth(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration
            .GetSection(ApiKeySettings.ConfigurationSectionName)
            .Get<ApiKeySettings>() ?? new ApiKeySettings();
        settings.Validate();
        builder.Services.AddSingleton(settings);

        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                AuthenticationSchemes.ApiKey,
                null);

        return builder;
    }
}
