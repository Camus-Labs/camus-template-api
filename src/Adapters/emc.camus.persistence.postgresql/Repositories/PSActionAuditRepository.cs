using Dapper;
using emc.camus.application.Common;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing action audit logs in PostgreSQL.
/// Provides methods to write explicit business actions to the audit table.
/// </summary>
internal sealed class PSActionAuditRepository : IActionAuditRepository
{
    private readonly PSUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSActionAuditRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="userContext">User context for capturing current user information.</param>
    public PSActionAuditRepository(
        PSUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(userContext);

        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <summary>
    /// Writes an action to the audit log using the current authenticated user context.
    /// This method captures the current user ID, username, and trace ID automatically.
    /// </summary>
    /// <param name="actionTitle">A short title describing the action (e.g., "User Login", "Role Assigned").</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    /// <exception cref="ArgumentException">Thrown when actionTitle or actionSummary is null, empty, or whitespace.</exception>
    public async Task<long> LogCurrentUserActionAsync(
        string actionTitle,
        string actionSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionSummary);

        var userId = _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("User ID is not available. Ensure the user is authenticated.");
        var username = _userContext.GetCurrentUsername()
            ?? throw new InvalidOperationException("Username is not available. Ensure the user is authenticated.");

        return await LogActionAsync(userId, username, actionTitle, actionSummary);
    }

    /// <summary>
    /// Writes an action to the audit log with explicit user information.
    /// </summary>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="username">The username performing the action (e.g., "System", "Migration").</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <returns>The ID of the created audit entry.</returns>
    public async Task<long> LogActionAsync(
        Guid userId,
        string username,
        string actionTitle,
        string actionSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionSummary);

        var connection = await _unitOfWork.GetConnectionAsync();

        var traceId = _userContext.GetCurrentTraceId();

        const string fkCheckSql = "SELECT EXISTS (SELECT 1 FROM camus.users WHERE id = @UserId)";
        var userExists = await connection.ExecuteScalarAsync<bool>(fkCheckSql, new { UserId = userId });

        if (!userExists)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");
        }

        const string sql = @"
            INSERT INTO camus.action_audit (user_id, user_name, trace_id, action_title, action_summary)
            VALUES (@UserId, @Username, @TraceId, @ActionTitle, @ActionSummary)
            RETURNING id";

        var auditId = await connection.ExecuteScalarAsync<long>(sql, new
        {
            userId,
            username,
            traceId,
            actionTitle,
            actionSummary
        });

        return auditId;
    }
}
