using System.Diagnostics.CodeAnalysis;

namespace emc.camus.security.jwt.Exceptions;

/// <summary>
/// Thrown when the JWT adapter fails to load or parse the RSA signing key.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class JwtKeyLoadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtKeyLoadException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The underlying exception that caused the key load failure.</param>
    public JwtKeyLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
