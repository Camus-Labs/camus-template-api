using emc.camus.application.Common;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Services;

/// <summary>
/// No-op unit of work for the in-memory persistence adapter.
/// All transaction operations are skipped because in-memory data does not require transactional guarantees.
/// </summary>
internal sealed partial class UnitOfWork : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "In-memory unit of work: {Operation} skipped — no database connection required.")]
    private partial void LogSkippedOperation(string operation);

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="logger">Logger for unit of work events.</param>
    public UnitOfWork(ILogger<UnitOfWork> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <summary>
    /// No-op: in-memory persistence does not require transactions.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A completed task.</returns>
    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        LogSkippedOperation("BeginTransaction");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No-op: in-memory persistence does not require transactions.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A completed task.</returns>
    public Task CommitAsync(CancellationToken ct = default)
    {
        LogSkippedOperation("Commit");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No-op: in-memory persistence does not require transactions.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task RollbackAsync()
    {
        LogSkippedOperation("Rollback");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No-op: in-memory persistence is always reachable.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A completed task.</returns>
    public Task CheckConnectivityAsync(CancellationToken ct = default)
    {
        LogSkippedOperation("CheckConnectivity");
        return Task.CompletedTask;
    }
}
