using System.Data;
using emc.camus.application.Common;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of action audit repository.
/// This is a no-op implementation that does not persist audit logs.
/// </summary>
public class IMActionAuditRepository : IActionAuditRepository
{
    private readonly ILogger<IMActionAuditRepository> _logger;

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
        _logger.LogInformation(
            "Audit Log: {ActionTitle} - {ActionSummary}",
            actionTitle,
            actionSummary ?? "No details");

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
        _logger.LogInformation(
            "System Audit Log: {Username} ({UserId}) - {ActionTitle} - {ActionSummary}",
            username,
            userId?.ToString() ?? "System",
            actionTitle,
            actionSummary ?? "No details");

        // Return a dummy ID
        return Task.FromResult(0L);
    }
}
