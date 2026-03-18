namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration for an API info entry.
/// </summary>
public class ApiInfoSettings
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateName();
        ValidateVersion();
        ValidateStatus();
        ValidateFeatures();
    }

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"API Name cannot be null or empty.");
        }
    }

    private void ValidateVersion()
    {
        if (string.IsNullOrWhiteSpace(Version))
        {
            throw new InvalidOperationException($"API Version cannot be null or empty.");
        }
    }

    private void ValidateStatus()
    {
        if (string.IsNullOrWhiteSpace(Status))
        {
            throw new InvalidOperationException($"API Status cannot be null or empty.");
        }
    }

    private void ValidateFeatures()
    {
        if (Features == null)
        {
            throw new InvalidOperationException("Features list cannot be null.");
        }
    }
}
