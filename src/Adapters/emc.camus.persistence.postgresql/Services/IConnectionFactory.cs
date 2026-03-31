using System.Data.Common;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Factory for creating database connections.
/// Implementations handle provider-specific connection creation and configuration.
/// </summary>
internal interface IConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection.
    /// Connection is configured with appropriate session context (user, trace ID, etc.).
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An open database connection ready for use.</returns>
    Task<DbConnection> CreateConnectionAsync(CancellationToken ct = default);
}
