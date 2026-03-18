using System.Security.Claims;
using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Extension methods for mapping between Domain entities and Application-layer views/types for Auth.
/// </summary>
public static class AuthMappingExtensions
{
    /// <summary>
    /// Converts a <see cref="GeneratedToken"/> domain entity to a <see cref="GeneratedTokenSummaryView"/>.
    /// </summary>
    /// <param name="token">The generated token domain entity.</param>
    /// <returns>A summary view of the generated token.</returns>
    public static GeneratedTokenSummaryView ToSummaryView(this GeneratedToken token)
    {
        return new GeneratedTokenSummaryView(
            token.Jti,
            token.TokenUsername,
            token.Permissions,
            token.ExpiresOn,
            token.CreatedAt,
            token.IsRevoked,
            token.RevokedAt,
            token.IsActive()
        );
    }

    /// <summary>
    /// Builds a list of permission claims from a <see cref="User"/> domain entity.
    /// </summary>
    /// <param name="user">The user domain entity.</param>
    /// <returns>A list of claims representing the user's permissions.</returns>
    public static List<Claim> ToPermissionClaims(this User user)
    {
        return user.GetPermissions()
            .Select(p => new Claim(Permissions.ClaimType, p))
            .ToList();
    }
}
