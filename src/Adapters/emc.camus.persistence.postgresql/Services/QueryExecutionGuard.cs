using emc.camus.persistence.postgresql.Exceptions;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Helper for wrapping database query execution with consistent exception handling.
/// Catches infrastructure failures and wraps them in
/// <see cref="DatabaseQueryException"/> to prevent raw infrastructure exceptions from
/// leaking beyond the adapter boundary.
/// </summary>
internal static class QueryExecutionGuard
{
    /// <summary>
    /// Executes an asynchronous database operation and wraps provider failures.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">A descriptive name for error context.</param>
    /// <returns>The result of the database operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is null or whitespace.</exception>
    /// <exception cref="DatabaseQueryException">Thrown when the query fails due to a provider-level error.</exception>
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        try
        {
            return await operation();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DatabaseQueryException(
                $"Database query failed during '{operationName}'.", ex);
        }
    }

    /// <summary>
    /// Executes an asynchronous database operation with no return value and wraps provider failures.
    /// </summary>
    /// <param name="operation">The database operation to execute.</param>
    /// <param name="operationName">A descriptive name for error context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is null or whitespace.</exception>
    /// <exception cref="DatabaseQueryException">Thrown when the query fails due to a provider-level error.</exception>
    public static async Task ExecuteAsync(Func<Task> operation, string operationName)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        try
        {
            await operation();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DatabaseQueryException(
                $"Database query failed during '{operationName}'.", ex);
        }
    }
}
