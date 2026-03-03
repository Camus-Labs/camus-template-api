namespace emc.camus.api.Models.Responses;

/// <summary>
/// A paginated response envelope containing items and pagination metadata.
/// Used as the Data payload inside ApiResponse for list endpoints.
/// </summary>
/// <typeparam name="T">The type of the items in the result set.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// The total number of items matching the query (across all pages).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage { get; set; }
}
