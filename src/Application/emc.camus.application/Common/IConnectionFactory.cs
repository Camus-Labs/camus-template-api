using System.Data;

namespace emc.camus.application.Common;

/// <summary>
/// Factory for creating database connections.
/// Implementations handle provider-specific connection creation and configuration.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection.
    /// Connection is configured with appropriate session context (user, trace ID, etc.).
    /// </summary>
    /// <returns>An open database connection ready for use.</returns>
    Task<IDbConnection> CreateConnectionAsync();
}
