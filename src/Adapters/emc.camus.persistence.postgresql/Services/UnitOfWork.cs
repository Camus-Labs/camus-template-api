using System.Data.Common;
using emc.camus.application.Common;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// PostgreSQL unit of work that manages a shared database connection and transaction
/// across all repositories within a single request scope.
/// Implements <see cref="IAsyncDisposable"/> so the DI container cleans up the connection at scope end.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    public UnitOfWork(IConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns the current database connection, creating one lazily if needed.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An open database connection.</returns>
    internal async Task<DbConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        _connection ??= await _connectionFactory.CreateConnectionAsync(ct);
        return _connection;
    }

    /// <summary>
    /// Opens a connection (if not already open) and begins a new transaction.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        _transaction = await connection.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            await _transaction.CommitAsync(ct);
    }

    /// <summary>
    /// Rolls back the current transaction. Uses <see cref="CancellationToken.None"/>
    /// because rollback is a compensating action that must run to completion.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync(CancellationToken.None);
    }

    /// <summary>
    /// Releases the transaction and connection resources asynchronously.
    /// </summary>
    /// <returns>A value task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
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
