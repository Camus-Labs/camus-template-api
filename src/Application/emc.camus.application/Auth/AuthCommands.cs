using System.Security.Claims;

namespace emc.camus.application.Auth;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
/// <param name="Username">The username for authentication.</param>
/// <param name="Password">The password for authentication.</param>
public record AuthenticateUserCommand(
    string Username, 
    string Password
);

/// <summary>
/// Command to generate a custom token with specific permissions and expiration.
/// </summary>
/// <param name="UsernameSuffix">The suffix to append to the current username (up to 20 chars, alphanumeric + . - _ only).</param>
/// <param name="ExpiresOn">The custom expiration date (UTC). Must be between 1 hour and 1 year from now.</param>
/// <param name="Permissions">The list of permissions to grant to the token. Must be a subset of the current user's permissions.</param>
public record GenerateTokenCommand(
    string UsernameSuffix,
    DateTime ExpiresOn,
    List<string> Permissions
);

/// <summary>
/// Command to revoke a generated token by its JTI (JWT ID).
/// </summary>
/// <param name="Jti">The JWT ID of the token to revoke.</param>
public record RevokeTokenCommand(
    Guid Jti
);
