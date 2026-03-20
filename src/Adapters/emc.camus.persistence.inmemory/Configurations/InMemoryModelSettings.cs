using emc.camus.application.Auth;

namespace emc.camus.persistence.inmemory.Configurations;

/// <summary>
/// Configuration settings for in-memory model data.
/// Contains all domain data (roles, users, API info) loaded from configuration
/// when the application uses in-memory storage instead of a database.
/// </summary>
public class InMemoryModelSettings
{
    /// <summary>
    /// Gets the configuration section name for in-memory model settings.
    /// </summary>
    public const string ConfigurationSectionName = "InMemoryModelSettings";
    /// <summary>
    /// Gets or sets the list of role definitions.
    /// </summary>
    public List<RoleSettings> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of user definitions.
    /// </summary>
    public List<UserSettings> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of API info definitions.
    /// </summary>
    public List<ApiInfoSettings> ApiInfos { get; set; } = new();

    /// <summary>
    /// Validates the in-memory persistence settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateRoles();
        ValidateUsers();
        ValidateApiInfos();
    }

    private void ValidateRoles()
    {
        if (Roles == null || Roles.Count == 0)
        {
            throw new InvalidOperationException($"At least one role must be defined in InMemoryModelSettings.Roles. Got: {Roles?.Count ?? 0} role(s).");
        }

        var roleNames = new HashSet<string>();

        foreach (var role in Roles)
        {
            role.Validate();

            if (roleNames.Contains(role.Name))
            {
                throw new InvalidOperationException($"Duplicate role name: {role.Name}");
            }

            roleNames.Add(role.Name);
        }
    }

    private void ValidateUsers()
    {
        if (Users == null || Users.Count == 0)
        {
            throw new InvalidOperationException($"At least one user must be defined in InMemoryModelSettings.Users. Got: {Users?.Count ?? 0} user(s).");
        }

        var availableRoles = Roles.Select(r => r.Name).ToList();

        foreach (var user in Users)
        {
            user.Validate(availableRoles);
        }
    }

    private void ValidateApiInfos()
    {
        if (ApiInfos == null)
        {
            throw new InvalidOperationException("ApiInfos cannot be null.");
        }

        var versionKeys = new HashSet<string>();

        foreach (var apiInfo in ApiInfos)
        {
            apiInfo.Validate();

            var key = apiInfo.Version.ToLowerInvariant();
            if (versionKeys.Contains(key))
            {
                throw new InvalidOperationException($"Duplicate API version: {apiInfo.Version}");
            }

            versionKeys.Add(key);
        }
    }
}
