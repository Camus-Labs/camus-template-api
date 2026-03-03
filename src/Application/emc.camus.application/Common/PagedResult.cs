namespace emc.camus.application.Common;

/// <summary>
/// Represents a page of results from a query, with metadata for pagination.
/// Used as output across application services to standardize paginated responses.
/// </summary>
/// <typeparam name="T">The type of the items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// The total number of items matching the query (across all pages).
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// The maximum number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a new paged result.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items matching the query.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    public PagedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>An empty paged result with zero total count.</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 25)
        => new(new List<T>(), 0, page, pageSize);
}
