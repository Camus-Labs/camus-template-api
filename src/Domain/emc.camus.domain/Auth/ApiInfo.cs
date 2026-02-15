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
    public List<string> Features { get; set; } = GetDefaultFeatures();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiInfo"/> class.
    /// </summary>
    /// <param name="version">The API version.</param>
    /// <param name="statusDetail">Optional status detail descriptor (e.g., "API Key", "JWT", "Basic Auth").</param>
    /// <param name="features">Optional list of features. If not provided, returns basic features for non-implemented versions.</param>
    /// <param name="name">Optional API name. If not provided, uses default name.</param>
    /// <exception cref="ArgumentException">Thrown when version is null, empty, or whitespace.</exception>
    public ApiInfo(string version, string? statusDetail = null, List<string>? features = null, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("API version is required and cannot be empty or whitespace.", nameof(version));
        }

        Name = name ?? DefaultApiName;
        Version = version;
        Status = FormatStatus(version, statusDetail);
        Features = features ?? GetDefaultFeatures();
    }

    private static string FormatStatus(string version, string? statusDetail)
    {
        var baseStatus = $"Running with API Versioning v{version}";
        return statusDetail != null ? $"{baseStatus} ({statusDetail})" : baseStatus;
    }

    private static List<string> GetDefaultFeatures()
    {
        return new List<string>
        {
            "Basic API"
        };
    }
}
