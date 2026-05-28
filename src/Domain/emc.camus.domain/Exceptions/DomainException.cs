using System.Diagnostics.CodeAnalysis;

namespace emc.camus.domain.Exceptions;

/// <summary>
/// Thrown when a domain business invariant is violated.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/> with the specified error message.
    /// </summary>
    /// <param name="message">The message that describes the invariant violation.</param>
    public DomainException(string message) : base(message) { }
}
