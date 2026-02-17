using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for application data provider selection.
/// </summary>
public class AppDataSettings
{
    /// <summary>
    /// Gets the configuration section name for application data settings.
    /// </summary>
    public const string ConfigurationSectionName = "AppDataSettings";

    /// <summary>
    /// Gets or sets the application data provider type.
    /// </summary>
    public AppDataProvider Provider { get; set; } = AppDataProvider.InMemory;

    /// <summary>
    /// Gets or sets the in-memory application data settings.
    /// </summary>
    public InMemoryAppDataSettings InMemory { get; set; } = new();

    /// <summary>
    /// Validates the application data settings.
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
        if (!Enum.IsDefined(typeof(AppDataProvider), Provider))
        {
            throw new ArgumentException($"Invalid application data provider: {Provider}. Must be 'InMemory' or 'Database'.", nameof(Provider));
        }
    }

    private void ValidateProviderSettings()
    {
        switch (Provider)
        {
            case AppDataProvider.InMemory:
                InMemory.Validate();
                break;

            default:
                throw new ArgumentException($"Unsupported provider: {Provider}", nameof(Provider));
        }
    }
}
