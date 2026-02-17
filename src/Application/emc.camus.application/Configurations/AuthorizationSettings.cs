using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for authorization provider selection.
/// </summary>
public class AuthorizationSettings
{
    /// <summary>
    /// Gets the configuration section name for authorization settings.
    /// </summary>
    public const string ConfigurationSectionName = "AuthorizationSettings";

    /// <summary>
    /// Gets or sets the authorization provider type.
    /// </summary>
    public AuthorizationProvider Provider { get; set; } = AuthorizationProvider.InMemory;

    /// <summary>
    /// Gets or sets the in-memory authorization settings.
    /// </summary>
    public InMemoryAuthorizationSettings InMemory { get; set; } = new();

    /// <summary>
    /// Validates the authorization settings.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateProvider();
        ValidateProviderSettings();
    }

    private void ValidateProvider()
    {
        if (!Enum.IsDefined(typeof(AuthorizationProvider), Provider))
        {
            throw new ArgumentException($"Invalid authorization provider: {Provider}. Must be 'InMemory' or 'Database'.", nameof(Provider));
        }
    }

    private void ValidateProviderSettings()
    {
        switch (Provider)
        {
            case AuthorizationProvider.InMemory:
                InMemory.Validate();
                break;

            default:
                throw new ArgumentException($"Unsupported provider: {Provider}", nameof(Provider));
        }
    }
}
