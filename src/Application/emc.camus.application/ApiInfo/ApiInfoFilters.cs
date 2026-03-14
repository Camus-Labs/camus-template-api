namespace emc.camus.application.ApiInfo;

/// <summary>
/// Filter for querying API information by version.
/// </summary>
public record ApiInfoFilter
{
    /// <summary>The API version to retrieve (e.g., "1.0", "2.0").</summary>
    public string Version { get; }

    /// <summary>
    /// Creates a new API info filter.
    /// </summary>
    /// <param name="version">The API version to query.</param>
    public ApiInfoFilter(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Version = version;
    }
}
