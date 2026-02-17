namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a role in the system that can be assigned to users for authorization.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of permissions assigned to this role.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="permissions">Optional list of permissions.</param>
    /// <param name="id">Optional unique identifier. If not provided, a new GUID will be generated.</param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
    public Role(string name, string? description = null, List<string>? permissions = null, string? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id ?? Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        Permissions = permissions ?? new List<string>();
    }
}
