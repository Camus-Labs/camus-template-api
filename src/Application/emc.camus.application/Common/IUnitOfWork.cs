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
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync();
}
