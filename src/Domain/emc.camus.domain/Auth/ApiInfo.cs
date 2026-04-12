namespace emc.camus.domain.Auth;

/// <summary>
/// Model representing API information for documentation and status endpoints.
/// </summary>
public class ApiInfo
{

    /// <summary>
    /// The name of the API.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The version of the API.
    /// </summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>
    /// The current status of the API.
    /// </summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>
    /// List of features available in this API version.
    /// </summary>
    public IReadOnlyList<string> Features { get; private set; } = new List<string>();

    /// <summary>
    /// Creates a new API info. Validates business attributes.
    /// </summary>
    /// <param name="name">The API name.</param>
    /// <param name="version">The API version.</param>
    /// <param name="status">The status detail descriptor (e.g., "Available", "Deprecated", "Beta").</param>
    /// <param name="features">Optional list of features. If not provided, returns empty list.</param>
    /// <exception cref="ArgumentException">Thrown when name, version, or status is null, empty, or whitespace.</exception>
    public ApiInfo(string name, string version, string status, List<string>? features = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Name = name;
        Version = version;
        Status = status;
        Features = features ?? new List<string>();
    }

    /// <summary>
    /// Private constructor for reconstitution from persistence.
    /// </summary>
    private ApiInfo() { }

    /// <summary>
    /// Rebuilds an API info from persistence data. Skips business validation.
    /// </summary>
    /// <param name="name">The API name.</param>
    /// <param name="version">The API version.</param>
    /// <param name="status">The status descriptor.</param>
    /// <param name="features">The list of features.</param>
    public static ApiInfo Reconstitute(string name, string version, string status, List<string> features)
    {
        return new ApiInfo
        {
            Name = name,
            Version = version,
            Status = status,
            Features = features
        };
    }
}
