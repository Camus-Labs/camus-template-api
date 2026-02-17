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
    /// <exception cref="ArgumentException">
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
            throw new ArgumentException("AdminSecretName cannot be null or empty.", nameof(AdminSecretName));
        }

        if (AdminSecretName.Length > MaxSecretNameLength)
        {
            throw new ArgumentException($"AdminSecretName cannot exceed {MaxSecretNameLength} characters.", nameof(AdminSecretName));
        }
    }

    private void ValidatePasswordSecretName()
    {
        if (string.IsNullOrWhiteSpace(PasswordSecretName))
        {
            throw new ArgumentException("PasswordSecretName cannot be null or empty.", nameof(PasswordSecretName));
        }

        if (PasswordSecretName.Length > MaxSecretNameLength)
        {
            throw new ArgumentException($"PasswordSecretName cannot exceed {MaxSecretNameLength} characters.", nameof(PasswordSecretName));
        }
    }
}
