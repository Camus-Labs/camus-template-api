using System.Diagnostics.CodeAnalysis;

namespace emc.camus.security.jwt.Exceptions;

/// <summary>
/// Thrown when the JWT adapter fails to generate a signed token.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class JwtTokenGenerationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenGenerationException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The underlying exception that caused the token generation failure.</param>
    public JwtTokenGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
