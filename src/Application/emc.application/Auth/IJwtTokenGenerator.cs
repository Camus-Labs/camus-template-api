using System.Security.Claims;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides functionality for generating JWT tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token with the specified claims.
    /// </summary>
    /// <param name="subject">The subject (user identifier) for the token.</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>A <see cref="JwtTokenResult"/> containing the token and expiration information.</returns>
    JwtTokenResult GenerateToken(string subject, IEnumerable<Claim>? additionalClaims = null);
}
