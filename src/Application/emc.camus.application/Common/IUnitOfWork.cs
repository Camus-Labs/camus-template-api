namespace emc.camus.application.Common;

/// <summary>
/// Abstracts transactional boundaries for application services.
/// PostgreSQL adapters manage a real database transaction; in-memory adapters no-op.
/// Scoped lifetime: one instance per HTTP request, shared across all repositories in the same scope.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Opens a connection (if not already open) and begins a new transaction.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Rolls back the current transaction. Not cancellable — rollback must run to completion.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync();
}
