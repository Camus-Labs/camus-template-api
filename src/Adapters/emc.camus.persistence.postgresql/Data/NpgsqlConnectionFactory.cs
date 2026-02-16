using System.Data;
using emc.camus.application.Configurations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace emc.camus.persistence.postgresql.Data;

/// <summary>
/// PostgreSQL implementation of database connection factory using Npgsql.
/// </summary>
public class NpgsqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="settings">Database settings containing connection configuration.</param>
    /// <param name="logger">Logger for connection factory events.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings or logger is null.</exception>
    /// <exception cref="ArgumentException">Thrown when connection string is invalid.</exception>
    public NpgsqlConnectionFactory(
        DatabaseSettings settings,
        ILogger<NpgsqlConnectionFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings), "Database settings cannot be null.");
        }

        settings.Validate();

        _connectionString = settings.ConnectionString;
    }

    /// <summary>
    /// Creates and opens a new PostgreSQL database connection.
    /// </summary>
    /// <returns>An open database connection.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection cannot be opened.
    /// </exception>
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            _logger.LogDebug("Database connection opened successfully");
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open database connection");
            throw new InvalidOperationException("Failed to open database connection. Ensure the database is accessible and the connection string is correct.", ex);
        }
    }
}
