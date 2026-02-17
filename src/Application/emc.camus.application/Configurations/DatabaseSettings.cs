using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for database connections.
/// Supports both static connection strings and dynamic credentials from secret providers.
/// </summary>
public class DatabaseSettings
{
    private const int MaxSecretNameLength = 50;
    private const int MaxAdditionalParametersLength = 100;
    private const int MinPort = 1;
    private const int MaxPort = 65535;
    
    /// <summary>
    /// Gets the configuration section name for database settings.
    /// </summary>
    public const string ConfigurationSectionName = "DatabaseSettings";

    /// <summary>
    /// Gets or sets the database provider type.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSQL;

    /// <summary>
    /// Gets or sets the database host (e.g., localhost, db.example.com).
    /// Required when using secret-based credentials.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database port.
    /// Default: 5432 for PostgreSQL.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the database name.
    /// Required when using secret-based credentials.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret containing the database username.
    /// Required.
    /// </summary>
    public string UserSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret containing the database password.
    /// Required.
    /// </summary>
    public string PasswordSecretName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional connection string parameters (e.g., "SslMode=Require;Pooling=true").
    /// These are appended to the dynamically built connection string.
    /// </summary>
    public string? AdditionalParameters { get; set; }

    /// <summary>
    /// Validates the database settings.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateProvider();
        ValidateHost();
        ValidatePort();
        ValidateDatabase();
        ValidateUserSecretName();
        ValidatePasswordSecretName();
        ValidateAdditionalParameters();
    }

    private void ValidateProvider()
    {
        if (!Enum.IsDefined(typeof(DatabaseProvider), Provider))
        {
            throw new ArgumentException($"Invalid database provider: {Provider}.", nameof(Provider));
        }
    }

    private void ValidateHost()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("Host cannot be null or empty.", nameof(Host));
        }
    }

    private void ValidatePort()
    {
        if (Port < MinPort || Port > MaxPort)
        {
            throw new ArgumentException($"Port must be between {MinPort} and {MaxPort}. Current value: {Port}", nameof(Port));
        }
    }

    private void ValidateDatabase()
    {
        if (string.IsNullOrWhiteSpace(Database))
        {
            throw new ArgumentException("Database cannot be null or empty.", nameof(Database));
        }
    }

    private void ValidateUserSecretName()
    {
        if (string.IsNullOrWhiteSpace(UserSecretName))
        {
            throw new ArgumentException("UserSecretName cannot be null or empty.", nameof(UserSecretName));
        }

        if (UserSecretName.Length > MaxSecretNameLength)
        {
            throw new ArgumentException(
                $"UserSecretName must not exceed {MaxSecretNameLength} characters. Current length: {UserSecretName.Length}", 
                nameof(UserSecretName));
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
            throw new ArgumentException(
                $"PasswordSecretName must not exceed {MaxSecretNameLength} characters. Current length: {PasswordSecretName.Length}", 
                nameof(PasswordSecretName));
        }
    }

    private void ValidateAdditionalParameters()
    {
        if (AdditionalParameters != null && AdditionalParameters.Length > MaxAdditionalParametersLength)
        {
            throw new ArgumentException(
                $"AdditionalParameters must not exceed {MaxAdditionalParametersLength} characters. Current length: {AdditionalParameters.Length}", 
                nameof(AdditionalParameters));
        }
    }
}
