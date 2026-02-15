using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration for a role definition.
/// </summary>
public class RoleConfig
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
    /// <exception cref="ArgumentException">
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
            throw new ArgumentException("Name cannot be null or empty.", nameof(Name));
        }

        if (Name.Length > MaxRoleNameLength)
        {
            throw new ArgumentException($"Name must not exceed {MaxRoleNameLength} characters. Current length: {Name.Length}", nameof(Name));
        }
    }

    private void ValidatePermissions()
    {
        if (Permissions == null || Permissions.Count == 0)
        {
            throw new ArgumentException($"Role '{Name}' must have at least one permission.", nameof(Permissions));
        }

        var validPermissions = Auth.Permissions.GetAll();
        var invalidPermissions = Permissions.Where(p => !validPermissions.Contains(p)).ToList();
        
        if (invalidPermissions.Count > 0)
        {
            throw new ArgumentException($"Role '{Name}' has invalid permissions: {string.Join(", ", invalidPermissions)}. Valid permissions are: {string.Join(", ", validPermissions)}", nameof(Permissions));
        }
    }
}
