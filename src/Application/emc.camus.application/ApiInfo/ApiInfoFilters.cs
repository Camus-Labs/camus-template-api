using DomainApiInfo = emc.camus.domain.Auth.ApiInfo;

namespace emc.camus.application.ApiInfo;

/// <summary>
/// Filter for querying API information by version.
/// </summary>
public sealed record ApiInfoFilter
{
    /// <summary>The API version to retrieve (e.g., "1.0", "2.0").</summary>
    public string Version { get; }

    /// <summary>
    /// Creates a new API info filter.
    /// </summary>
    /// <param name="version">The API version to query.</param>
    public ApiInfoFilter(string version)
    {
        // Normalize version: "2" → "2.0" (URL segment may omit minor version)
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Version = version.Contains('.') ? version : $"{version}.0";
        ValidateVersion();
    }

    private void ValidateVersion()
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Version.Length, DomainApiInfo.MaxVersionLength, nameof(Version));
    }
}
