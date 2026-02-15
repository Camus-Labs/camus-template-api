using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for in-memory application data.
/// </summary>
public class InMemoryAppDataSettings
{
    /// <summary>
    /// Gets or sets the list of API info definitions.
    /// </summary>
    public List<ApiInfoConfig> ApiInfos { get; set; } = new();

    /// <summary>
    /// Validates the in-memory application data settings.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateApiInfos();
    }

    private void ValidateApiInfos()
    {
        if (ApiInfos == null)
        {
            throw new ArgumentException("ApiInfos cannot be null.", nameof(ApiInfos));
        }

        var versionKeys = new HashSet<string>();

        foreach (var apiInfo in ApiInfos)
        {
            apiInfo.Validate();

            var key = apiInfo.Version.ToLowerInvariant();
            if (versionKeys.Contains(key))
            {
                throw new ArgumentException($"Duplicate API version: {apiInfo.Version}", nameof(ApiInfos));
            }

            versionKeys.Add(key);
        }
    }
}
