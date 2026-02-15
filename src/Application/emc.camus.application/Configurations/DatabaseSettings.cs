using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for database connections.
/// </summary>
public class DatabaseSettings
{
    private const int MaxConnectionStringLength = 1000;

    /// <summary>
    /// Gets or sets the database provider type.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSQL;

    /// <summary>
    /// Gets or sets the connection string for the database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Validates the database settings.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateProvider();
        ValidateConnectionString();
    }

    private void ValidateProvider()
    {
        if (!Enum.IsDefined(typeof(DatabaseProvider), Provider))
        {
            throw new ArgumentException($"Invalid database provider: {Provider}.", nameof(Provider));
        }
    }

    private void ValidateConnectionString()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(ConnectionString));
        }

        if (ConnectionString.Length > MaxConnectionStringLength)
        {
            throw new ArgumentException($"ConnectionString must not exceed {MaxConnectionStringLength} characters. Current length: {ConnectionString.Length}", nameof(ConnectionString));
        }
    }
}
