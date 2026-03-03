namespace emc.camus.api.Models.Requests.V2;

/// <summary>
/// Request model for generating a custom token with specific permissions and expiration.
/// </summary>
public class GenerateTokenRequest
{
    /// <summary>
    /// The suffix to append to the current username (up to 20 characters).
    /// Allowed characters: alphanumeric, dots (.), hyphens (-), and underscores (_).
    /// </summary>
    public string UsernameSuffix { get; set; } = string.Empty;

    /// <summary>
    /// The custom expiration date and time (UTC).
    /// Must be between 1 hour and 1 year from now.
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    /// The list of permissions to grant to the token.
    /// Must be a subset of the current user's permissions.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
