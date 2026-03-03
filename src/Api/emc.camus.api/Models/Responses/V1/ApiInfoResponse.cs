namespace emc.camus.api.Models.Responses.V1;

/// <summary>
/// API response DTO for API information.
/// Contains API metadata including version, status, and available features.
/// </summary>
public sealed class ApiInfoResponse
{
    /// <summary>
    /// API version identifier (e.g., "1.0", "2.0")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Current API status or authentication requirement description
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// List of features available in this API version
    /// </summary>
    public required IReadOnlyList<string> Features { get; init; }
}
