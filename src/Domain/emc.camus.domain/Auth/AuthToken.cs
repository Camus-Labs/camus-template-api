namespace emc.camus.domain.Auth;

/// <summary>
/// Represents an authentication token with its expiration time.
/// </summary>
public class AuthToken
{
    /// <summary>
    /// Gets the token string.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the expiration date and time (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthToken"/> class.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="expiresOn">The expiration date and time (UTC).</param>
    /// <exception cref="ArgumentException">Thrown when token is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expiresOn is not in the future.</exception>
    public AuthToken(string token, DateTime expiresOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresOn, DateTime.UtcNow);

        Token = token;
        ExpiresOn = expiresOn;
    }

    /// <summary>
    /// Private constructor.
    /// </summary>
    private AuthToken() { }
}
