using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Mapping;

/// <summary>
/// Provides extension methods for mapping between GeneratedTokenModel (database) and GeneratedToken (domain).
/// </summary>
internal static class GeneratedTokenMappingExtensions
{
    /// <summary>
    /// Maps a GeneratedTokenModel to a GeneratedToken domain entity.
    /// </summary>
    /// <param name="model">The database model to map from.</param>
    /// <returns>A new GeneratedToken domain entity.</returns>
    public static GeneratedToken ToEntity(this GeneratedTokenModel model)
    {
        return GeneratedToken.Reconstitute(
            jti: model.Jti,
            creatorUserId: model.CreatorUserId,
            creatorUsername: model.CreatorUsername,
            tokenUsername: model.TokenUsername,
            permissions: model.Permissions?.ToList() ?? new List<string>(),
            expiresOn: model.ExpiresOn,
            createdAt: model.CreatedAt,
            isRevoked: model.IsRevoked,
            revokedAt: model.RevokedAt);
    }
}
