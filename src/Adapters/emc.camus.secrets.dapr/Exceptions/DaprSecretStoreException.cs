using System.Diagnostics.CodeAnalysis;

namespace emc.camus.secrets.dapr.Exceptions
{
    /// <summary>
    /// Represents a technology-level failure when communicating with the Dapr secret store
    /// (timeouts, connection refused, non-success HTTP status codes).
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class DaprSecretStoreException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DaprSecretStoreException"/> class.
        /// </summary>
        /// <param name="message">The error message describing the failure.</param>
        /// <param name="innerException">The underlying exception that caused this failure.</param>
        public DaprSecretStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
