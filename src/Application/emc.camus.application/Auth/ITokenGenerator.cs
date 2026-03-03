using System.Security.Claims;
using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides functionality for generating authentication tokens.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates an authentication token with default JTI and configured expiration.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="username">The username for the user.</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>An <see cref="AuthToken"/> containing the token and expiration information.</returns>
    AuthToken GenerateToken(Guid userId, string username, IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// Generates an authentication token with a specific JTI, custom expiration, and specified claims.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <param name="username">The username for the user.</param>
    /// <param name="jti">The JWT ID for unique token identification.</param>
    /// <param name="expiresOn">The custom expiration date and time (UTC).</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>An <see cref="AuthToken"/> containing the token and expiration information.</returns>
    AuthToken GenerateToken(Guid userId, string username, Guid jti, DateTime expiresOn, IEnumerable<Claim>? additionalClaims = null);
}
