using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Idempotency;

/// <summary>
/// Defines well-known values for the <see cref="Common.Headers.IdempotencyKeyStatus"/> response header.
/// </summary>
[ExcludeFromCodeCoverage]
public static class IdempotencyKeyStatuses
{
    /// <summary>
    /// Indicates the response was served from the idempotency cache (cache hit).
    /// </summary>
    public const string Hit = "hit";

    /// <summary>
    /// Indicates the action was executed and the response was cached for the first time (cache miss).
    /// </summary>
    public const string Miss = "miss";
}
