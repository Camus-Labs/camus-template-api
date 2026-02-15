using System.Security.Claims;

namespace emc.camus.application.Auth;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
/// <param name="Username">The username for authentication.</param>
/// <param name="Password">The password for authentication.</param>
public record AuthenticateUserCommand(string Username, string Password);

/// <summary>
/// Command to generate a token for an authenticated user.
/// </summary>
/// <param name="Subject">The subject (user identifier) for the token.</param>
/// <param name="AdditionalClaims">Optional additional claims to include in the token.</param>
public record GenerateTokenCommand(
    string Subject,
    IEnumerable<Claim>? AdditionalClaims = null
);
