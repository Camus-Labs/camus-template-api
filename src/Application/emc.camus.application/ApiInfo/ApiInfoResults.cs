namespace emc.camus.application.ApiInfo;

/// <summary>
/// Result record returned by ApiInfoService after retrieving API information.
/// Contains API metadata including version, status, and available features.
/// </summary>
/// <param name="Version">API version identifier (e.g., "1.0", "2.0")</param>
/// <param name="Status">Current API status or authentication requirement description</param>
/// <param name="Features">List of features available in this API version</param>
public sealed record ApiInfoResults(
    string Version,
    string Status,
    IReadOnlyList<string> Features
);
