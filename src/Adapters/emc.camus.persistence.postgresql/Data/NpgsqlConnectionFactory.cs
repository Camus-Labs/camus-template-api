using System.Data;
using Dapper;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace emc.camus.persistence.postgresql.Data;

/// <summary>
/// PostgreSQL implementation of database connection factory using Npgsql.
/// Automatically sets session variables for user context to enable
/// automatic audit field population via database triggers.
/// </summary>
public class NpgsqlConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="settings">Database settings containing connection configuration.</param>
    /// <param name="logger">Logger for connection factory events.</param>
    /// <param name="userContext">User context for audit tracking.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings, logger, or userContext is null.</exception>
    /// <exception cref="ArgumentException">Thrown when connection string is invalid.</exception>
    public NpgsqlConnectionFactory(
        DatabaseSettings settings,
        ILogger<NpgsqlConnectionFactory> logger,
        IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userContext);

        _logger = logger;
        _userContext = userContext;
        _connectionString = settings.ConnectionString;
    }

    /// <summary>
    /// Creates and opens a new PostgreSQL database connection with user context set.
    /// Session variable app.current_username is automatically configured for audit triggers
    /// to populate created_by and updated_by fields with the authenticated user's username.
    /// </summary>
    /// <returns>An open database connection with session context configured.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection cannot be opened.
    /// </exception>
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Set session variables for audit triggers
            await SetSessionContextAsync(connection);
            
            _logger.LogDebug("Database connection opened successfully");
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open database connection");
            throw new InvalidOperationException("Failed to open database connection. Ensure the database is accessible and the connection string is correct.", ex);
        }
    }

    /// <summary>
    /// Sets PostgreSQL session variable with current user context.
    /// This variable is used by database triggers to automatically populate audit fields.
    /// Uses session-scoped SET (not SET LOCAL) since we're outside a transaction block.
    /// Variable persists for the lifetime of this connection.
    /// </summary>
    private async Task SetSessionContextAsync(IDbConnection connection)
    {
        var username = _userContext.GetCurrentUsername();

        if (!string.IsNullOrEmpty(username))
        {
            await connection.ExecuteAsync(
                "SET app.current_username = @Username;",
                new { Username = username });
        }
    }
}

