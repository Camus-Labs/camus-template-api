using System.Data;
using Dapper;
using emc.camus.application.Common;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing action audit logs in PostgreSQL.
/// Provides methods to write explicit business actions to the audit table.
/// </summary>
public class PSActionAuditRepository : IActionAuditRepository
{
    private readonly ILogger<PSActionAuditRepository> _logger;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSActionAuditRepository"/> class.
    /// </summary>
    /// <param name="logger">Logger for repository events.</param>
    /// <param name="userContext">User context for capturing current user information.</param>
    public PSActionAuditRepository(
        ILogger<PSActionAuditRepository> logger,
        IUserContext userContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Writes an action to the audit log using the provided database connection.
    /// This method captures the current user context and trace ID automatically.
    /// </summary>
    /// <param name="connection">The database connection to use (must be open).</param>
    /// <param name="actionTitle">A short title describing the action (e.g., "User Login", "Role Assigned").</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actionTitle is empty or whitespace.</exception>
    public async Task<long> LogActionAsync(
        IDbConnection connection,
        string actionTitle,
        string? actionSummary = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        
        if (string.IsNullOrWhiteSpace(actionTitle))
            throw new ArgumentException("Action title cannot be empty or whitespace.", nameof(actionTitle));

        try
        {
            var userId = _userContext.GetCurrentUserId();
            var username = _userContext.GetCurrentUsername();
            var traceId = _userContext.GetCurrentTraceId();

            const string sql = @"
                INSERT INTO action_audit (user_id, user_name, trace_id, action_title, action_summary)
                VALUES (@UserId, @Username, @TraceId, @ActionTitle, @ActionSummary)
                RETURNING id";

            var auditId = await connection.ExecuteScalarAsync<long>(sql, new
            {
                UserId = userId,
                Username = username,
                TraceId = traceId,
                ActionTitle = actionTitle,
                ActionSummary = actionSummary
            });

            _logger.LogDebug(
                "Audit entry created: Id={AuditId}, Action={ActionTitle}, User={Username}, TraceId={TraceId}",
                auditId, actionTitle, username ?? "System", traceId ?? "N/A");

            return auditId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create audit entry for action: {ActionTitle}", actionTitle);
            throw;
        }
    }

    /// <summary>
    /// Writes an action to the audit log with explicit user information (for system operations).
    /// </summary>
    /// <param name="connection">The database connection to use (must be open).</param>
    /// <param name="userId">The user ID performing the action (null for system operations).</param>
    /// <param name="username">The username performing the action (e.g., "System", "Migration").</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">Optional detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    public async Task<long> LogSystemActionAsync(
        IDbConnection connection,
        Guid? userId,
        string username,
        string actionTitle,
        string? actionSummary = null)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        
        if (string.IsNullOrWhiteSpace(actionTitle))
            throw new ArgumentException("Action title cannot be empty or whitespace.", nameof(actionTitle));
        
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty or whitespace.", nameof(username));

        try
        {
            var traceId = _userContext.GetCurrentTraceId();

            const string sql = @"
                INSERT INTO action_audit (user_id, user_name, trace_id, action_title, action_summary)
                VALUES (@UserId, @Username, @TraceId, @ActionTitle, @ActionSummary)
                RETURNING id";

            var auditId = await connection.ExecuteScalarAsync<long>(sql, new
            {
                UserId = userId,
                Username = username,
                TraceId = traceId,
                ActionTitle = actionTitle,
                ActionSummary = actionSummary
            });

            _logger.LogDebug(
                "System audit entry created: Id={AuditId}, Action={ActionTitle}, User={Username}",
                auditId, actionTitle, username);

            return auditId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create system audit entry for action: {ActionTitle}", actionTitle);
            throw;
        }
    }
}
