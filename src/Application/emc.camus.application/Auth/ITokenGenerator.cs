using System.Security.Claims;
using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides functionality for generating authentication tokens.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates an authentication token with the specified claims.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="username">The username for the user.</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>An <see cref="AuthToken"/> containing the token and expiration information.</returns>
    AuthToken GenerateToken(string userId, string username, IEnumerable<Claim>? additionalClaims = null);
}
