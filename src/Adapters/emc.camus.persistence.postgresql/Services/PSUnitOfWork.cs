using System.Data;
using emc.camus.application.Common;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// PostgreSQL unit of work that manages a shared database connection and transaction
/// across all repositories within a single request scope.
/// Implements <see cref="IAsyncDisposable"/> so the DI container cleans up the connection at scope end.
/// </summary>
internal sealed class PSUnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSUnitOfWork"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    public PSUnitOfWork(IConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns the current database connection, creating one lazily if needed.
    /// </summary>
    /// <returns>An open database connection.</returns>
    internal async Task<IDbConnection> GetConnectionAsync()
    {
        _connection ??= await _connectionFactory.CreateConnectionAsync();
        return _connection;
    }

    /// <summary>
    /// Opens a connection (if not already open) and begins a new transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync()
    {
        var connection = await GetConnectionAsync();
        _transaction = connection.BeginTransaction();
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task CommitAsync()
    {
        _transaction?.Commit();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RollbackAsync()
    {
        _transaction?.Rollback();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the transaction and connection resources asynchronously.
    /// </summary>
    /// <returns>A value task representing the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Releases the transaction and connection resources.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
        _connection?.Dispose();
        _connection = null;
    }
}
