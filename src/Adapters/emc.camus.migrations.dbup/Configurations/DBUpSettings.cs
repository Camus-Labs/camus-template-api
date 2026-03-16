namespace emc.camus.migrations.dbup.Configurations;

/// <summary>
/// Configuration settings for DbUp database migrations.
/// Uses DatabaseSettings for Host/Port/Database configuration.
/// </summary>
public class DBUpSettings
{
    private const int MaxSecretNameLength = 200;

    /// <summary>
    /// Gets the configuration section name for DbUp settings.
    /// </summary>
    public const string ConfigurationSectionName = "DBUpSettings";

    /// <summary>
    /// Gets or sets the name of the secret containing the database admin username.
    /// Required.
    /// </summary>
    public string AdminSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret containing the database admin password.
    /// Required.
    /// </summary>
    public string PasswordSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Validates the DbUp settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateAdminSecretName();
        ValidatePasswordSecretName();
    }

    private void ValidateAdminSecretName()
    {
        if (string.IsNullOrWhiteSpace(AdminSecretName))
        {
            throw new InvalidOperationException("AdminSecretName cannot be null or empty.");
        }

        if (AdminSecretName.Length > MaxSecretNameLength)
        {
            throw new InvalidOperationException($"AdminSecretName cannot exceed {MaxSecretNameLength} characters.");
        }
    }

    private void ValidatePasswordSecretName()
    {
        if (string.IsNullOrWhiteSpace(PasswordSecretName))
        {
            throw new InvalidOperationException("PasswordSecretName cannot be null or empty.");
        }

        if (PasswordSecretName.Length > MaxSecretNameLength)
        {
            throw new InvalidOperationException($"PasswordSecretName cannot exceed {MaxSecretNameLength} characters.");
        }
    }
}
