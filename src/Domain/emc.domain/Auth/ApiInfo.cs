namespace emc.camus.domain.Auth;

/// <summary>
/// Model representing API information for documentation and status endpoints.
/// </summary>
public class ApiInfo
{
    /// <summary>
    /// The name of the API.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The version of the API.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the API.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// List of features available in this API version.
    /// </summary>
    public List<string>? Features { get; set; }

    /// <summary>
    /// The timestamp of the API status or info (UTC).
    /// </summary>
    public DateTime? Timestamp { get; set; }
}
