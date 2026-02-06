namespace emc.camus.application.Auth;

/// <summary>
/// Represents the result of a JWT token generation operation.
/// </summary>
public class JwtTokenResult
{
    /// <summary>
    /// The generated JWT token string.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// The UTC date and time when the token expires.
    /// </summary>
    public required DateTime ExpiresOn { get; set; }
}
