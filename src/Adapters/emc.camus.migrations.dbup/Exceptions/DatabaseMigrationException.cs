using System.Diagnostics.CodeAnalysis;

namespace emc.camus.migrations.dbup.Exceptions;

/// <summary>
/// Exception thrown when a database migration fails due to infrastructure-level errors.
/// Wraps underlying technology failures (connection timeouts, refused connections, DbUp errors)
/// while preserving the original exception as <see cref="Exception.InnerException"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DatabaseMigrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseMigrationException"/> with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the migration failure.</param>
    /// <param name="innerException">The underlying exception that caused the migration to fail.</param>
    public DatabaseMigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
