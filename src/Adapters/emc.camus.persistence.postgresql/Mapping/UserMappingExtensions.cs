using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Mapping;

/// <summary>
/// Provides extension methods for mapping between UserModel (database) and User (domain).
/// </summary>
internal static class UserMappingExtensions
{
    /// <summary>
    /// Maps a UserModel and collection of RoleModels to a User domain entity.
    /// </summary>
    /// <param name="userModel">The user database model.</param>
    /// <param name="roleModels">The collection of role database models.</param>
    /// <returns>A new User domain entity with mapped roles.</returns>
    public static User ToEntity(this UserModel userModel, IEnumerable<RoleModel> roleModels)
    {
        var roles = roleModels.Select(r => r.ToEntity()).ToList();
        return User.Reconstitute(userModel.Id, userModel.Username, roles);
    }
}
