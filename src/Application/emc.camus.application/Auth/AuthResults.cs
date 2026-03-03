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
/// Result of a successful token generation operation.
/// </summary>
/// <param name="Token">The generated authentication token.</param>
/// <param name="ExpiresOn">The expiration date and time of the token (UTC).</param>
/// <param name="RequestorUserId">The ID of the user who requested the token.</param>
/// <param name="RequestorUsername">The username of the user who requested the token.</param>
/// <param name="TokenUsername">The username associated with the token (includes suffix).</param>
public record GenerateTokenResult(
    string Token,
    DateTime ExpiresOn,
    Guid RequestorUserId,
    string RequestorUsername,
    string TokenUsername
);
