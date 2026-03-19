using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Mapping;

/// <summary>
/// Provides extension methods for mapping between RoleModel (database) and Role (domain).
/// </summary>
public static class RoleMappingExtensions
{
    /// <summary>
    /// Maps a RoleModel to a Role domain entity.
    /// </summary>
    /// <param name="model">The database model to map from.</param>
    /// <returns>A new Role domain entity.</returns>
    public static Role ToEntity(this RoleModel model)
    {
        return Role.Reconstitute(
            id: model.Id,
            name: model.Name,
            description: model.Description,
            permissions: model.Permissions?.ToList() ?? new List<string>());
    }
}
