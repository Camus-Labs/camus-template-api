namespace emc.camus.domain.Auth;

/// <summary>
/// Model representing API information for documentation and status endpoints.
/// </summary>
public class ApiInfo
{
    /// <summary>Maximum allowed length for the API name (matches DB VARCHAR constraint).</summary>
    public const int MaxNameLength = 200;

    /// <summary>Maximum allowed length for the API version (matches DB VARCHAR constraint).</summary>
    public const int MaxVersionLength = 50;

    /// <summary>Maximum allowed length for the API status (matches DB VARCHAR constraint).</summary>
    public const int MaxStatusLength = 100;

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
        ValidateName(name);
        ValidateVersion(version);
        ValidateStatus(status);

        Name = name;
        Version = version;
        Status = status;
        Features = features ?? new List<string>();
    }

    /// <summary>
    /// Validates the API name is non-empty and within length constraints.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when name exceeds max length.</exception>
    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, MaxNameLength, nameof(name));
    }

    /// <summary>
    /// Validates the API version is non-empty and within length constraints.
    /// </summary>
    /// <param name="version">The version to validate.</param>
    /// <exception cref="ArgumentException">Thrown when version is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when version exceeds max length.</exception>
    private static void ValidateVersion(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version.Length, MaxVersionLength, nameof(version));
    }

    /// <summary>
    /// Validates the API status is non-empty and within length constraints.
    /// </summary>
    /// <param name="status">The status to validate.</param>
    /// <exception cref="ArgumentException">Thrown when status is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when status exceeds max length.</exception>
    private static void ValidateStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(status.Length, MaxStatusLength, nameof(status));
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
