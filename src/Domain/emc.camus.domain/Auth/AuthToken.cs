namespace emc.camus.domain.Auth;

/// <summary>
/// Represents an authentication token with its expiration time.
/// </summary>
public class AuthToken
{
    /// <summary>
    /// Gets the token string.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Gets the expiration date and time (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthToken"/> class.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="expiresOn">The expiration date and time (UTC).</param>
    /// <exception cref="ArgumentException">Thrown when token is null or whitespace, or expiresOn is in the past.</exception>
    public AuthToken(string token, DateTime expiresOn)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));

        if (expiresOn <= DateTime.UtcNow)
            throw new ArgumentException("Token expiration must be in the future.", nameof(expiresOn));

        Token = token;
        ExpiresOn = expiresOn;
    }
}
