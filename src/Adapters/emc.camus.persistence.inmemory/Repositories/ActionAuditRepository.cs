using emc.camus.application.Common;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of action audit repository.
/// This is a no-op implementation that does not persist audit logs.
/// </summary>
internal sealed partial class ActionAuditRepository : IActionAuditRepository
{
    private readonly ILogger<ActionAuditRepository> _logger;
    private readonly IUserContext _userContext;

    [LoggerMessage(Level = LogLevel.Information,
        Message = "System Audit Log: {Username} ({UserId}) - {ActionTitle} - {ActionSummary}")]
    private partial void LogSystemAuditAction(string username, string userId, string actionTitle, string actionSummary);

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionAuditRepository"/> class.
    /// </summary>
    /// <param name="logger">Logger for repository events.</param>
    /// <param name="userContext">User context for capturing current user information.</param>
    public ActionAuditRepository(
        ILogger<ActionAuditRepository> logger,
        IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userContext);

        _logger = logger;
        _userContext = userContext;
    }

    /// <summary>
    /// Logs an action to the application logs using the current authenticated user context.
    /// This method captures the current user context automatically.
    /// </summary>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A dummy audit entry ID of 0.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="actionTitle"/> or <paramref name="actionSummary"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the authenticated user context does not provide a valid user ID or username.</exception>
    public Task<long> LogCurrentUserActionAsync(
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

        return LogActionAsync(userId, username, actionTitle, actionSummary, ct);
    }

    /// <summary>
    /// Logs an action to the application logs with explicit user information.
    /// </summary>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="username">The username performing the action.</param>
    /// <param name="actionTitle">A short title describing the action.</param>
    /// <param name="actionSummary">A detailed summary of what was done.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A dummy audit entry ID of 0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="userId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="username"/>, <paramref name="actionTitle"/>, or <paramref name="actionSummary"/> is null or whitespace.
    /// </exception>
    public Task<long> LogActionAsync(
        Guid userId,
        string username,
        string actionTitle,
        string actionSummary,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionSummary);
        // Log to application logs instead of database
        LogSystemAuditAction(
            username,
            userId.ToString(),
            actionTitle,
            actionSummary);

        // Return a dummy ID
        return Task.FromResult(0L);
    }
}
