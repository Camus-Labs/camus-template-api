namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a role in the system that can be assigned to users for authorization.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets the unique identifier for the role.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of the role.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the list of permissions assigned to this role.
    /// </summary>
    public List<string> Permissions { get; private set; } = new();

    /// <summary>
    /// Creates a new role. Validates business attributes and auto-generates ID when null.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="permissions">Optional list of permissions.</param>
    /// <param name="id">Optional unique identifier. If not provided, a new GUID will be generated.</param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
    public Role(string name, string? description = null, List<string>? permissions = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id ?? Guid.NewGuid();
        Name = name;
        Description = description;
        Permissions = permissions ?? new List<string>();
    }

    /// <summary>
    /// Private constructor for reconstitution from persistence.
    /// </summary>
    private Role() { }

    /// <summary>
    /// Rebuilds a role from persistence data. Skips business validation.
    /// </summary>
    public static Role Reconstitute(Guid id, string name, string? description, List<string> permissions)
    {
        return new Role
        {
            Id = id,
            Name = name,
            Description = description,
            Permissions = permissions
        };
    }
}
