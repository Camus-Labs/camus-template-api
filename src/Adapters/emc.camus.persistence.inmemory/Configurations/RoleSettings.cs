using emc.camus.application.Auth;

namespace emc.camus.persistence.inmemory.Configurations;

/// <summary>
/// Configuration for a role definition.
/// </summary>
internal sealed class RoleSettings
{
    private const int MaxRoleNameLength = 50;

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of permissions for this role.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Validates the role configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any property is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateName();
        ValidatePermissions();
    }

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Name cannot be null or empty.");
        }

        if (Name.Length > MaxRoleNameLength)
        {
            throw new InvalidOperationException($"Name must not exceed {MaxRoleNameLength} characters. Current length: {Name.Length}");
        }
    }

    private void ValidatePermissions()
    {
        if (Permissions == null || Permissions.Count == 0)
        {
            throw new InvalidOperationException($"Role '{Name}' must have at least one permission.");
        }

        var validPermissions = emc.camus.application.Auth.Permissions.GetAll();
        var invalidPermissions = Permissions.Where(p => !validPermissions.Contains(p)).ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new InvalidOperationException($"Role '{Name}' has invalid permissions: {string.Join(", ", invalidPermissions)}. Valid permissions are: {string.Join(", ", validPermissions)}");
        }
    }
}
