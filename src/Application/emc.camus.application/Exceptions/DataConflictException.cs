namespace emc.camus.application.Exceptions;

/// <summary>
/// Exception thrown when a data constraint violation is detected (e.g., uniqueness, referential integrity).
/// Thrown by repositories to signal that a persistence operation conflicts with existing data.
/// Maps to HTTP 409 Conflict in the exception handling middleware.
/// </summary>
public sealed class DataConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataConflictException"/> class.
    /// </summary>
    /// <param name="message">The message describing the constraint violation.</param>
    public DataConflictException(string message)  : base(message) { }
}
