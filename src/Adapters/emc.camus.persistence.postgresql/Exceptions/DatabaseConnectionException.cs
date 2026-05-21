using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.postgresql.Exceptions;

/// <summary>
/// Exception thrown when a database connection cannot be established.
/// Wraps underlying connection failures (timeouts, refused connections, network errors)
/// while preserving the original exception as <see cref="Exception.InnerException"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DatabaseConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConnectionException"/> class
    /// with a descriptive message and the inner exception that caused this connection failure.
    /// </summary>
    /// <param name="message">A message describing the connection failure context.</param>
    /// <param name="innerException">The underlying exception that caused the connection failure.</param>
    public DatabaseConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
