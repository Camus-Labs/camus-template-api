namespace emc.camus.api.Models.Responses.V2;

/// <summary>
/// Response model containing generated token information and permissions.
/// </summary>
public class GenerateTokenResponse
{
    /// <summary>
    /// The generated authentication token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    /// The username associated with the token (includes suffix).
    /// Format: {OriginalUsername}-{Suffix}
    /// </summary>
    public string TokenUsername { get; set; } = string.Empty;
}
