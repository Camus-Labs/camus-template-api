namespace emc.camus.application.Auth;

/// <summary>
/// Result of a successful user authentication operation.
/// </summary>
public sealed record AuthenticateUserResult
{
    /// <summary>The generated authentication token.</summary>
    public string Token { get; }

    /// <summary>The expiration date and time of the token (UTC).</summary>
    public DateTime ExpiresOn { get; }

    /// <summary>
    /// Creates a new authentication result.
    /// </summary>
    /// <param name="token">The generated authentication token.</param>
    /// <param name="expiresOn">The expiration date and time of the token (UTC).</param>
    public AuthenticateUserResult(string token, DateTime expiresOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        ExpiresOn = expiresOn;
    }
}

/// <summary>
/// Result of a successful token generation operation.
/// </summary>
public sealed record GenerateTokenResult
{
    /// <summary>The generated authentication token.</summary>
    public string Token { get; }

    /// <summary>The expiration date and time of the token (UTC).</summary>
    public DateTime ExpiresOn { get; }

    /// <summary>The ID of the user who requested the token.</summary>
    public Guid RequestorUserId { get; }

    /// <summary>The username of the user who requested the token.</summary>
    public string RequestorUsername { get; }

    /// <summary>The username associated with the token (includes suffix).</summary>
    public string TokenUsername { get; }

    /// <summary>
    /// Creates a new token generation result.
    /// </summary>
    /// <param name="token">The generated authentication token.</param>
    /// <param name="expiresOn">The expiration date and time of the token (UTC).</param>
    /// <param name="requestorUserId">The ID of the user who requested the token.</param>
    /// <param name="requestorUsername">The username of the user who requested the token.</param>
    /// <param name="tokenUsername">The username associated with the token (includes suffix).</param>
    public GenerateTokenResult(string token, DateTime expiresOn, Guid requestorUserId, string requestorUsername, string tokenUsername)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentOutOfRangeException.ThrowIfEqual(requestorUserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestorUsername);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUsername);
        Token = token;
        ExpiresOn = expiresOn;
        RequestorUserId = requestorUserId;
        RequestorUsername = requestorUsername;
        TokenUsername = tokenUsername;
    }
}
