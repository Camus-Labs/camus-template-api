using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.Models.Responses;

/// <summary>
/// Represents the detailed health check response containing overall status and per-check results.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HealthCheckDetailedResponse
{
    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the collection of individual health check results.
    /// </summary>
    public required IEnumerable<HealthCheckEntryResponse> Checks { get; init; }
}

/// <summary>
/// Represents a single health check entry result.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HealthCheckEntryResponse
{
    /// <summary>
    /// Gets the name of the health check.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the status of the health check.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the description of the health check result.
    /// </summary>
    public required string? Description { get; init; }

    /// <summary>
    /// Gets the duration of the health check execution in milliseconds.
    /// </summary>
    public required double Duration { get; init; }
}
