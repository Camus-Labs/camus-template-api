namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a user in the system with authentication and authorization information.
/// </summary>
public class User
{
    /// <summary>
    /// Gets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the username or access key.
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the list of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<Role> Roles { get; private set; } = new List<Role>();

    /// <summary>
    /// Creates a new user. Validates business attributes and auto-generates ID when null.
    /// </summary>
    /// <param name="username">The username for the user.</param>
    /// <param name="roles">Optional list of roles assigned to the user.</param>
    /// <param name="id">Optional unique identifier. If not provided, a new GUID will be generated.</param>
    /// <exception cref="ArgumentException">Thrown when username is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when id is empty.</exception>
    public User(string username, List<Role>? roles = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        if (id.HasValue)
            ArgumentOutOfRangeException.ThrowIfEqual(id.Value, Guid.Empty);

        Id = id ?? Guid.NewGuid();
        Username = username;
        Roles = roles ?? new List<Role>();
    }

    /// <summary>
    /// Private constructor for reconstitution from persistence.
    /// </summary>
    private User() { }

    /// <summary>
    /// Rebuilds a user from persistence data. Skips business validation.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="username">The username.</param>
    /// <param name="roles">The list of roles.</param>
    public static User Reconstitute(Guid id, string username, List<Role> roles)
    {
        return new User
        {
            Id = id,
            Username = username,
            Roles = roles
        };
    }

    /// <summary>
    /// Gets all unique permissions from all roles assigned to this user.
    /// </summary>
    /// <returns>A distinct list of permission strings.</returns>
    public List<string> GetPermissions()
    {
        return Roles
            .SelectMany(role => role.Permissions)
            .Distinct()
            .ToList();
    }
}
