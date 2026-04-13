using emc.camus.application.Common;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing action audit logs in PostgreSQL.
/// Delegates raw SQL execution to <see cref="IActionAuditDataAccess"/>.
/// </summary>
internal sealed class ActionAuditRepository : IActionAuditRepository
{
    private readonly UnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;
    private readonly IActionAuditDataAccess _dataAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionAuditRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="userContext">User context for capturing current user information.</param>
    /// <param name="dataAccess">Data access layer for raw SQL execution.</param>
    public ActionAuditRepository(
        UnitOfWork unitOfWork,
        IUserContext userContext,
        IActionAuditDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(dataAccess);

        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _dataAccess = dataAccess;
    }

    /// <summary>
    /// Writes an action to the audit log using the current authenticated user context.
    /// This method captures the current user ID, username, and trace ID automatically.
    /// </summary>
    /// <param name="actionTitle">A short title describing the action (e.g., "User Login", "Role Assigned").</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The ID of the created audit entry.</returns>
    /// <exception cref="ArgumentException">Thrown when actionTitle or actionSummary is null, empty, or whitespace.</exception>
    public async Task<long> LogCurrentUserActionAsync(
        string actionTitle,
        string actionSummary,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionSummary);

        var userId = _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException("User ID is not available. Ensure the user is authenticated.");
        var username = _userContext.GetCurrentUsername()
            ?? throw new InvalidOperationException("Username is not available. Ensure the user is authenticated.");

        return await LogActionAsync(userId, username, actionTitle, actionSummary, ct);
    }

    /// <summary>
    /// Writes an action to the audit log with explicit user information.
    /// </summary>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="username">The username performing the action (e.g., "System", "Migration").</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The ID of the created audit entry.</returns>
    public async Task<long> LogActionAsync(
        Guid userId,
        string username,
        string actionTitle,
        string actionSummary,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionSummary);

        var connection = await _unitOfWork.GetConnectionAsync(ct);
        var traceId = _userContext.GetCurrentTraceId();

        var userExists = await _dataAccess.UserExistsAsync(connection, userId, ct);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");
        }

        return await _dataAccess.InsertAsync(connection, userId, username, traceId, actionTitle, actionSummary, ct);
    }
}
