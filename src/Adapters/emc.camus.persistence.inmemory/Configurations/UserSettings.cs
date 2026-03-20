namespace emc.camus.persistence.inmemory.Configurations;

/// <summary>
/// Configuration for a user definition.
/// </summary>
internal sealed class UserSettings
{
    private const int MaxSecretNameLength = 50;

    /// <summary>
    /// Gets or sets the secret name that contains the username for authentication.
    /// This is a reference to a secret in the secret store (e.g., Dapr Secret Store).
    /// </summary>
    public string UsernameSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret name that contains the password for authentication.
    /// This is a reference to a secret in the secret store (e.g., Dapr Secret Store).
    /// </summary>
    public string PasswordSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of role names assigned to this user.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Validates the user configuration against available roles.
    /// </summary>
    /// <param name="availableRoles">List of valid role names.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="availableRoles"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any property is invalid.
    /// </exception>
    public void Validate(List<string> availableRoles)
    {
        ValidateUsernameSecretName();
        ValidatePasswordSecretName();
        ValidateRoles(availableRoles);
    }

    private void ValidateUsernameSecretName()
    {
        if (string.IsNullOrWhiteSpace(UsernameSecretName))
        {
            throw new InvalidOperationException($"UsernameSecretName cannot be null or empty.");
        }

        if (UsernameSecretName.Length > MaxSecretNameLength)
        {
            throw new InvalidOperationException($"UsernameSecretName must not exceed {MaxSecretNameLength} characters. Current length: {UsernameSecretName.Length}");
        }
    }

    private void ValidatePasswordSecretName()
    {
        if (string.IsNullOrWhiteSpace(PasswordSecretName))
        {
            throw new InvalidOperationException($"User with UsernameSecretName '{UsernameSecretName}' must have a PasswordSecretName.");
        }

        if (PasswordSecretName.Length > MaxSecretNameLength)
        {
            throw new InvalidOperationException($"PasswordSecretName must not exceed {MaxSecretNameLength} characters. Current length: {PasswordSecretName.Length}");
        }
    }

    private void ValidateRoles(List<string> availableRoles)
    {
        ArgumentNullException.ThrowIfNull(availableRoles);

        if (Roles == null || Roles.Count == 0)
        {
            throw new InvalidOperationException($"User with UsernameSecretName '{UsernameSecretName}' must have at least one role.");
        }

        var invalidRoles = Roles.Where(r => !availableRoles.Contains(r)).ToList();

        if (invalidRoles.Count > 0)
        {
            throw new InvalidOperationException($"User with UsernameSecretName '{UsernameSecretName}' has invalid roles: {string.Join(", ", invalidRoles)}. Valid roles are: {string.Join(", ", availableRoles)}");
        }
    }
}
