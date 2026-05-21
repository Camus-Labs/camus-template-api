using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.postgresql.Models;

/// <summary>
/// Data model representing the existence check results for required database tables.
/// Used by Dapper for ORM mapping of schema verification queries.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TableExistenceModel
{
    /// <summary>
    /// Gets or sets whether the users table exists.
    /// </summary>
    public bool UsersExists { get; set; }

    /// <summary>
    /// Gets or sets whether the roles table exists.
    /// </summary>
    public bool RolesExists { get; set; }

    /// <summary>
    /// Gets or sets whether the user_roles table exists.
    /// </summary>
    public bool UserRolesExists { get; set; }

    /// <summary>
    /// Gets or sets whether the role_permissions table exists.
    /// </summary>
    public bool RolePermissionsExists { get; set; }
}
