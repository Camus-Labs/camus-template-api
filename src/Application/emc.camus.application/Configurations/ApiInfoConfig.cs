using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration for an API info entry.
/// </summary>
public class ApiInfoConfig
{
    /// <summary>
    /// Gets or sets the name of the API.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the API.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status description.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of features.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Validates the API info configuration.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("API Name cannot be null or empty.", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            throw new ArgumentException("API Version cannot be null or empty.", nameof(Version));
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            throw new ArgumentException("API Status cannot be null or empty.", nameof(Status));
        }

        if (Features == null)
        {
            throw new ArgumentException("Features list cannot be null.", nameof(Features));
        }
    }
}
