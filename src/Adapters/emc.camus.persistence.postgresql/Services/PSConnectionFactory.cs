using System.Data.Common;
using Dapper;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.application.Secrets;
using Npgsql;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// PostgreSQL implementation of database connection factory using Npgsql.
/// Automatically sets session variables for user context to enable
/// automatic audit field population via database triggers.
/// Supports both static connection strings and secret-based credentials.
/// </summary>
internal sealed class PSConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSConnectionFactory"/> class.
    /// </summary>
    /// <param name="settings">Database settings containing connection configuration.</param>
    /// <param name="userContext">User context for audit tracking.</param>
    /// <param name="secretProvider">Secret provider for fetching credentials.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings, userContext, or secretProvider is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when secrets cannot be retrieved.</exception>
    public PSConnectionFactory(
        DatabaseSettings settings,
        IUserContext userContext,
        ISecretProvider secretProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(secretProvider);

        _userContext = userContext;

        // Build connection string from secret-based credentials
        _connectionString = BuildConnectionString(settings, secretProvider);
    }

    /// <summary>
    /// Creates and opens a new PostgreSQL database connection with user context set.
    /// Session variable app.current_username is automatically configured for audit triggers
    /// to populate created_by and updated_by fields with the authenticated user's username.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An open database connection with session context configured.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection cannot be opened.
    /// </exception>
    public async Task<DbConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            // Set session variables for audit triggers
            await SetSessionContextAsync(connection, ct);

            return connection;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to open database connection. Ensure the database is accessible and the connection string is correct.", ex);
        }
    }

    /// <summary>
    /// Sets PostgreSQL session variable with current user context.
    /// This variable is used by database triggers to automatically populate audit fields.
    /// Uses session-scoped SET (not SET LOCAL) since we're outside a transaction block.
    /// Variable persists for the lifetime of this connection.
    /// </summary>
    /// <param name="connection">The open database connection to configure.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    private async Task SetSessionContextAsync(DbConnection connection, CancellationToken ct)
    {
        var username = _userContext.GetCurrentUsername();

        if (!string.IsNullOrEmpty(username))
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "SET app.current_username = @Username;",
                    new { Username = username },
                    cancellationToken: ct));
        }
    }

    /// <summary>
    /// Builds the complete connection string using credentials from secret provider.
    /// </summary>
    /// <param name="settings">Database settings containing connection configuration.</param>
    /// <param name="secretProvider">Secret provider to fetch credentials.</param>
    /// <returns>The complete PostgreSQL connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when secrets cannot be retrieved.</exception>
    private static string BuildConnectionString(DatabaseSettings settings, ISecretProvider secretProvider)
    {
        // Fetch credentials from secret provider
        var username = secretProvider.GetSecret(settings.UserSecretName);
        var password = secretProvider.GetSecret(settings.PasswordSecretName);

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                $"Database username secret '{settings.UserSecretName}' not found or empty");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Database password secret '{settings.PasswordSecretName}' not found or empty");
        }

        // Build connection string from components
        var connectionString = $"Host={settings.Host};Port={settings.Port};Database={settings.Database};Username={username};Password={password}";

        if (!string.IsNullOrWhiteSpace(settings.AdditionalParameters))
        {
            connectionString += $";{settings.AdditionalParameters}";
        }

        return connectionString;
    }
}

