using System.Data;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// Thin data access layer for action audit SQL operations.
/// </summary>
internal interface IActionAuditDataAccess
{
    /// <summary>
    /// Checks whether a user with the given ID exists.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if the user exists; otherwise false.</returns>
    Task<bool> UserExistsAsync(IDbConnection connection, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Inserts an audit log entry and returns its ID.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="username">The username performing the action.</param>
    /// <param name="traceId">The trace ID for correlation.</param>
    /// <param name="actionTitle">The action title.</param>
    /// <param name="actionSummary">The action summary.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The ID of the created audit entry.</returns>
    Task<long> InsertAsync(IDbConnection connection, Guid userId, string username, string? traceId, string actionTitle, string actionSummary, CancellationToken ct = default);
}
