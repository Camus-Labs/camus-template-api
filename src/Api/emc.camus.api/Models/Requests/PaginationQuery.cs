namespace emc.camus.api.Models.Requests;

/// <summary>
/// Query parameters for paginated list endpoints.
/// </summary>
public class PaginationQuery
{
    private const int DefaultPageSize = 25;

    /// <summary>
    /// The page number to retrieve (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page. Defaults to 25, maximum 100.
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
