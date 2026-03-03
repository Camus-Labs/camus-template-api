using System.ComponentModel.DataAnnotations;

namespace emc.camus.api.Models.Requests;

/// <summary>
/// Query parameters for paginated list endpoints.
/// </summary>
public class PaginationQuery
{
    /// <summary>
    /// The page number to retrieve (1-based). Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page. Defaults to 25, maximum 100.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 25;
}
