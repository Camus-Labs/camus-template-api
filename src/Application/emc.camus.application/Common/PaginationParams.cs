namespace emc.camus.application.Common;

/// <summary>
/// Represents validated pagination parameters for querying paged data.
/// Used as input across application services to standardize pagination requests.
/// Validation is enforced through the constructor — values are always clamped to valid ranges.
/// </summary>
public record PaginationParams
{
    /// <summary>
    /// Default page size when none is specified.
    /// </summary>
    public const int DefaultPageSize = 25;

    /// <summary>
    /// Maximum allowed page size to prevent excessive data retrieval.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Initializes a new instance of <see cref="PaginationParams"/> with validated values.
    /// Page is clamped to a minimum of 1. PageSize is clamped between 1 and <see cref="MaxPageSize"/>.
    /// </summary>
    /// <param name="page">The requested page number (clamped to >= 1).</param>
    /// <param name="pageSize">The requested page size (clamped between 1 and MaxPageSize).</param>
    public PaginationParams(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, MaxPageSize);
    }

    /// <summary>
    /// The requested page number (1-based). Always >= 1.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// The number of items per page. Always between 1 and <see cref="MaxPageSize"/>.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Calculates the number of items to skip for the current page.
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
}
