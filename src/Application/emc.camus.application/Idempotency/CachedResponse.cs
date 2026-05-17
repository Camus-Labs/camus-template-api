namespace emc.camus.application.Idempotency;

/// <summary>
/// Represents a cached HTTP response for an idempotent request.
/// Stores the information needed to replay the response on subsequent duplicate requests.
/// </summary>
public sealed class CachedResponse
{
    /// <summary>
    /// The HTTP status code of the original response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The serialized response body.
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// The SHA256 hash of the original request body used to detect body mismatches.
    /// </summary>
    public string BodyHash { get; }

    /// <summary>
    /// Creates a new cached response instance.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="body">The serialized response body.</param>
    /// <param name="bodyHash">The SHA256 hash of the request body.</param>
    public CachedResponse(int statusCode, string? body, string bodyHash)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(statusCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyHash);

        StatusCode = statusCode;
        Body = body;
        BodyHash = bodyHash;
    }
}
