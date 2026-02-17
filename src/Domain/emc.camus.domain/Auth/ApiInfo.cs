namespace emc.camus.domain.Auth;

/// <summary>
/// Model representing API information for documentation and status endpoints.
/// </summary>
public class ApiInfo
{
    private const string DefaultApiName = "My Basic API";

    /// <summary>
    /// The name of the API.
    /// </summary>
    public string Name { get; set; } = DefaultApiName;

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
    public List<string> Features { get; set; } = new List<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiInfo"/> class.
    /// </summary>
    /// <param name="version">The API version.</param>
    /// <param name="status">The status detail descriptor (e.g., "Available", "Deprecated", "Beta").</param>
    /// <param name="features">Optional list of features. If not provided, returns empty list.</param>
    /// <param name="name">Optional API name. If not provided, uses default name.</param>
    /// <exception cref="ArgumentException">Thrown when version or status is null, empty, or whitespace.</exception>
    public ApiInfo(string version, string status, List<string>? features = null, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Name = name ?? DefaultApiName;
        Version = version;
        Status = status;
        Features = features ?? new List<string>();
    }
}
