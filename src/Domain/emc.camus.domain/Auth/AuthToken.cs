namespace emc.camus.domain.Auth;

/// <summary>
/// Authentication token issued after successful credential verification.
/// </summary>
public class AuthToken
{
    /// <summary>
    /// The generated authentication token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; set; }
}
