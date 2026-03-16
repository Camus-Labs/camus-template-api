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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
