namespace emc.camus.application.ApiInfo;

/// <summary>
/// Detail view record for API information query results.
/// Contains API metadata including version, status, and available features.
/// </summary>
public sealed record ApiInfoDetailView
{
    /// <summary>API version identifier (e.g., "1.0", "2.0").</summary>
    public string Version { get; }

    /// <summary>Current API status or authentication requirement description.</summary>
    public string Status { get; }

    /// <summary>List of features available in this API version.</summary>
    public IReadOnlyList<string> Features { get; }

    /// <summary>
    /// Creates a new API info detail view.
    /// </summary>
    /// <param name="version">API version identifier (e.g., "1.0", "2.0").</param>
    /// <param name="status">Current API status or authentication requirement description.</param>
    /// <param name="features">List of features available in this API version.</param>
    public ApiInfoDetailView(string version, string status, IReadOnlyList<string> features)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(features);
        Version = version;
        Status = status;
        Features = features;
    }
}
