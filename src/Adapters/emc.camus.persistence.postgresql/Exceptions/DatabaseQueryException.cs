using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.postgresql.Exceptions;

/// <summary>
/// Exception thrown when a database query execution fails due to a technology-level error
/// (timeout, network interruption, protocol error). Wraps the underlying provider exception
/// while preserving it as <see cref="Exception.InnerException"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DatabaseQueryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseQueryException"/> class
    /// with a descriptive message and the inner exception that caused this query failure.
    /// </summary>
    /// <param name="message">A message describing the query failure context.</param>
    /// <param name="innerException">The underlying exception that caused the query failure.</param>
    public DatabaseQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
