namespace emc.camus.application.Auth;

/// <summary>
/// Result of a successful user authentication operation.
/// </summary>
/// <param name="Token">The generated authentication token.</param>
/// <param name="ExpiresOn">The expiration date and time of the token (UTC).</param>
public record AuthenticateUserResult(
    string Token,
    DateTime ExpiresOn
);

/// <summary>
/// Result of token generation operation.
/// </summary>
/// <param name="Token">The generated authentication token string.</param>
/// <param name="ExpiresOn">The expiration date and time of the token (UTC).</param>
public record GenerateTokenResult(string Token, DateTime ExpiresOn);
