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
    /// Validates the user configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any property is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateUsernameSecretName();
        ValidatePasswordSecretName();
        ValidateRoles();
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

    private void ValidateRoles()
    {
        if (Roles == null || Roles.Count == 0)
        {
            throw new InvalidOperationException($"User with UsernameSecretName '{UsernameSecretName}' must have at least one role.");
        }
    }
}
