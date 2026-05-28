using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.Configurations;

/// <summary>
/// Defines well-known idempotency TTL policy names.
/// These names correspond to policies configured in IdempotencySettings.
/// </summary>
[ExcludeFromCodeCoverage]
public static class IdempotencyPolicies
{
    /// <summary>
    /// Default idempotency policy with a 5-minute TTL window.
    /// </summary>
    public const string Default = "default";

    /// <summary>
    /// Long-term idempotency policy with a 24-hour TTL window.
    /// </summary>
    public const string LongTerm = "long-term";
}
