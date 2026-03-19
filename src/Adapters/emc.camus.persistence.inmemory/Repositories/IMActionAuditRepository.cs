using System.Data;
using emc.camus.application.Common;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of action audit repository.
/// This is a no-op implementation that does not persist audit logs.
/// </summary>
public partial class IMActionAuditRepository : IActionAuditRepository
{
    private readonly ILogger<IMActionAuditRepository> _logger;

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Audit Log: {ActionTitle} - {ActionSummary}")]
    private partial void LogAuditAction(string actionTitle, string actionSummary);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "System Audit Log: {Username} ({UserId}) - {ActionTitle} - {ActionSummary}")]
    private partial void LogSystemAuditAction(string username, string userId, string actionTitle, string actionSummary);

    /// <summary>
    /// Initializes a new instance of the <see cref="IMActionAuditRepository"/> class.
    /// </summary>
    /// <param name="logger">Logger for repository events.</param>
    public IMActionAuditRepository(ILogger<IMActionAuditRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        
        _logger = logger;
    }

    /// <summary>
    /// Logs an action to the application logs instead of persisting to a database.
    /// The connection parameter is ignored in this in-memory implementation.
    /// </summary>
    /// <param name="connection">The database connection (ignored for in-memory implementation).</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>A dummy audit entry ID of 0.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="actionTitle"/> is null or whitespace.</exception>
    public Task<long> LogActionAsync(
        IDbConnection connection,
        string actionTitle,
        string? actionSummary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        // Log to application logs instead of database
        LogAuditAction(actionTitle, actionSummary ?? "No details");

        // Return a dummy ID
        return Task.FromResult(0L);
    }

    /// <summary>
    /// Logs a system action to the application logs with explicit user information instead of persisting to a database.
    /// The connection parameter is ignored in this in-memory implementation.
    /// </summary>
    /// <param name="connection">The database connection (ignored for in-memory implementation).</param>
    /// <param name="userId">The user ID performing the action (null for system operations).</param>
    /// <param name="username">The username performing the action.</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>A dummy audit entry ID of 0.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="username"/> or <paramref name="actionTitle"/> is null or whitespace.
    /// </exception>
    public Task<long> LogSystemActionAsync(
        IDbConnection connection,
        Guid? userId,
        string username,
        string actionTitle,
        string? actionSummary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        // Log to application logs instead of database
        LogSystemAuditAction(
            username,
            userId?.ToString() ?? "System",
            actionTitle,
            actionSummary ?? "No details");

        // Return a dummy ID
        return Task.FromResult(0L);
    }
}
