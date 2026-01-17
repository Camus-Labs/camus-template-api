namespace emc.camus.domain.Auth;

/// <summary>
/// Authentication credentials for token generation.
/// </summary>
public class Credentials
{
    /// <summary>
    /// The access key for authentication.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// The access secret for authentication.
    /// </summary>
    public string? AccessSecret { get; set; }
}
