namespace emc.camus.api.Models.Responses.V2;

/// <summary>
/// Response model containing authentication token information.
/// </summary>
public class AuthenticateUserResponse
{
    /// <summary>
    /// The generated authentication token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; set; }
}
