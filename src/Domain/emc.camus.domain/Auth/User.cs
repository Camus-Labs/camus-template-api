namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a user in the system with authentication and authorization information.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username or access key.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public List<Role> Roles { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="username">The username for the user.</param>
    /// <param name="roles">Optional list of roles assigned to the user.</param>
    /// <param name="id">Optional unique identifier. If not provided, a new GUID will be generated.</param>
    /// <exception cref="ArgumentException">Thrown when username is null, empty, or whitespace.</exception>
    public User(string username, List<Role>? roles = null, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required and cannot be empty or whitespace.", nameof(username));
        }

        Id = id ?? Guid.NewGuid().ToString();
        Username = username;
        Roles = roles ?? new List<Role>();
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
