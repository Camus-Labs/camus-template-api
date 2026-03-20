namespace emc.camus.migrations.dbup.Configurations;

/// <summary>
/// Configuration settings for DbUp database migrations.
/// Uses DatabaseSettings for Host/Port/Database configuration.
/// </summary>
internal sealed class DBUpSettings
{
    private const int MaxSecretNameLength = 200;

    /// <summary>
    /// Gets the configuration section name for DbUp settings.
    /// </summary>
    public const string ConfigurationSectionName = "DBUpSettings";

    /// <summary>
    /// Gets or sets a value indicating whether database migrations are enabled.
    /// When <c>false</c>, <see cref="DatabaseMigrationSetupExtensions.AddDatabaseMigrations"/>
    /// and <see cref="DatabaseMigrationSetupExtensions.UseDatabaseMigrations"/> are no-ops.
    /// Default: <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret containing the database admin username.
    /// Required when <see cref="Enabled"/> is <c>true</c>.
    /// </summary>
    public string AdminSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret containing the database admin password.
    /// Required when <see cref="Enabled"/> is <c>true</c>.
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
        if (!Enabled)
        {
            return;
        }

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
