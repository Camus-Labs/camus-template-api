using emc.camus.api.Models.Requests;
using emc.camus.api.Models.Responses;
using emc.camus.application.Common;

namespace emc.camus.api.Mapping;

/// <summary>
/// Extension methods for mapping common/reusable types between API and Application layers.
/// Includes pagination mapping used across multiple endpoints.
/// </summary>
public static class CommonMappingExtensions
{
    /// <summary>
    /// Converts a PaginationQuery (API layer) to PaginationParams (Application layer).
    /// </summary>
    /// <param name="query">The pagination query from the API request.</param>
    /// <returns>Validated pagination parameters for the application layer.</returns>
    public static PaginationParams ToPaginationParams(this PaginationQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new PaginationParams(query.Page, query.PageSize);
    }

    /// <summary>
    /// Converts a PagedResult (Domain/Application) to a PagedResponse (API layer).
    /// Requires a mapping function to convert each item from TSource to TDestination.
    /// </summary>
    /// <typeparam name="TSource">The source item type from the application layer.</typeparam>
    /// <typeparam name="TDestination">The destination item type for the API response.</typeparam>
    /// <param name="pagedResult">The paged result from the application layer.</param>
    /// <param name="mapper">A function to map each item from source to destination type.</param>
    /// <returns>A PagedResponse DTO for the API layer.</returns>
    public static PagedResponse<TDestination> ToPagedResponse<TSource, TDestination>(
        this PagedResult<TSource> pagedResult,
        Func<TSource, TDestination> mapper)
    {
        ArgumentNullException.ThrowIfNull(pagedResult);
        ArgumentNullException.ThrowIfNull(mapper);

        return new PagedResponse<TDestination>
        {
            Items = pagedResult.Items.Select(mapper).ToList(),
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasNextPage = pagedResult.HasNextPage,
            HasPreviousPage = pagedResult.HasPreviousPage
        };
    }
}
