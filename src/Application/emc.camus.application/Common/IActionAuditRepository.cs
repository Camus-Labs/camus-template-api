using System.Data;

namespace emc.camus.application.Common;

/// <summary>
/// Repository for managing action audit logs.
/// Provides methods to write explicit business actions to the audit table.
/// </summary>
public interface IActionAuditRepository
{
    /// <summary>
    /// Writes an action to the audit log using the provided database connection.
    /// Captures the current user context and trace ID automatically.
    /// </summary>
    /// <param name="connection">The database connection to use (must be open).</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    Task<long> LogActionAsync(
        IDbConnection connection,
        string actionTitle,
        string? actionSummary = null);

    /// <summary>
    /// Writes an action to the audit log with explicit user information (for system operations).
    /// </summary>
    /// <param name="connection">The database connection to use (must be open).</param>
    /// <param name="userId">The user ID performing the action (null for system operations).</param>
    /// <param name="username">The username performing the action.</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    Task<long> LogSystemActionAsync(
        IDbConnection connection,
        Guid? userId,
        string username,
        string actionTitle,
        string? actionSummary = null);
}
