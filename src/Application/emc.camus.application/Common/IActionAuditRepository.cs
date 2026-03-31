namespace emc.camus.application.Common;

/// <summary>
/// Repository for managing action audit logs.
/// Provides methods to write explicit business actions to the audit table.
/// </summary>
public interface IActionAuditRepository
{
    /// <summary>
    /// Writes an action to the audit log using the current authenticated user context.
    /// Captures the current user ID, username, and trace ID automatically.
    /// </summary>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The ID of the created audit entry.</returns>
    Task<long> LogCurrentUserActionAsync(
        string actionTitle,
        string actionSummary,
        CancellationToken ct = default);

    /// <summary>
    /// Writes an action to the audit log with explicit user information.
    /// </summary>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="username">The username performing the action.</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The ID of the created audit entry.</returns>
    Task<long> LogActionAsync(
        Guid userId,
        string username,
        string actionTitle,
        string actionSummary,
        CancellationToken ct = default);
}
