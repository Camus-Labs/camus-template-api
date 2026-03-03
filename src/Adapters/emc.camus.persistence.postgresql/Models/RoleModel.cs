namespace emc.camus.persistence.postgresql.Models;

/// <summary>
/// Data model representing roles table structure in PostgreSQL with associated permissions.
/// Used by Dapper for ORM mapping from JOIN queries.
/// </summary>
public class RoleModel
{
    /// <summary>
    /// Gets or sets the role's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the role name (e.g., "Admin", "User").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of permissions associated with this role.
    /// Maps to PostgreSQL array aggregation from role_permissions table.
    /// </summary>
    public List<string>? Permissions { get; set; }
}
