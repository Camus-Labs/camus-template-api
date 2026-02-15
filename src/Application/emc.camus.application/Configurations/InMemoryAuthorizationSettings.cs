using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for in-memory authorization.
/// </summary>
public class InMemoryAuthorizationSettings
{
    /// <summary>
    /// Gets or sets the list of role definitions.
    /// </summary>
    public List<RoleConfig> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of user definitions.
    /// </summary>
    public List<UserConfig> Users { get; set; } = new();

    /// <summary>
    /// Validates the in-memory authorization settings.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateRoles();
        ValidateUsers();
    }

    private void ValidateRoles()
    {
        if (Roles == null || Roles.Count == 0)
        {
            throw new ArgumentException("At least one role must be defined in Authorization.InMemory.Roles.", nameof(Roles));
        }

        var roleNames = new HashSet<string>();

        foreach (var role in Roles)
        {
            role.Validate();

            if (roleNames.Contains(role.Name))
            {
                throw new ArgumentException($"Duplicate role name: {role.Name}", nameof(Roles));
            }

            roleNames.Add(role.Name);
        }
    }

    private void ValidateUsers()
    {
        if (Users == null || Users.Count == 0)
        {
            throw new ArgumentException("At least one user must be defined in Authorization.InMemory.Users.", nameof(Users));
        }

        var availableRoles = Roles.Select(r => r.Name).ToList();

        foreach (var user in Users)
        {
            user.Validate(availableRoles);
        }
    }
}
