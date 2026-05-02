using emc.camus.application.Common;

namespace emc.camus.application.Auth;

/// <summary>
/// Specifies the field by which generated tokens can be sorted.
/// </summary>
public enum GeneratedTokenSortField
{
    /// <summary>Sort by token username.</summary>
    TokenUsername,

    /// <summary>Sort by expiration date.</summary>
    ExpiresOn,

    /// <summary>Sort by creation date.</summary>
    CreatedAt,

    /// <summary>Sort by revocation date.</summary>
    RevokedAt
}

/// <summary>
/// Encapsulates optional sorting parameters for generated token queries.
/// When both values are null, no sorting is applied. Both field and direction must be specified together.
/// </summary>
public sealed record GeneratedTokenSortParams
{
    /// <summary>The field to sort by, or null when no sorting is requested.</summary>
    public GeneratedTokenSortField? Field { get; }

    /// <summary>The direction to sort in, or null when no sorting is requested.</summary>
    public SortDirection? Direction { get; }

    /// <summary>
    /// Creates a new instance of <see cref="GeneratedTokenSortParams"/> from raw string values,
    /// performing coercion and validation.
    /// </summary>
    /// <param name="sortBy">The sort field name (case-insensitive), or null.</param>
    /// <param name="sortDirection">The sort direction (case-insensitive), or null.</param>
    /// <exception cref="ArgumentException">Thrown when only one value is provided or values cannot be parsed to a valid enum member.</exception>
    public GeneratedTokenSortParams(string? sortBy = null, string? sortDirection = null)
    {
        if (sortBy is null && sortDirection is null)
        {
            return;
        }

        if (sortBy is null || sortDirection is null)
        {
            throw new ArgumentException("Both sortBy and sortDirection must be provided together.");
        }

        if (!Enum.TryParse<GeneratedTokenSortField>(sortBy, ignoreCase: true, out var parsedField))
        {
            throw new ArgumentException($"Invalid value for sortBy: '{sortBy}'. Allowed values: tokenUsername, expiresOn, createdAt, revokedAt.");
        }

        if (!Enum.TryParse<SortDirection>(sortDirection, ignoreCase: true, out var parsedDirection))
        {
            throw new ArgumentException($"Invalid value for sortDirection: '{sortDirection}'. Allowed values: asc, desc.");
        }

        Field = parsedField;
        Direction = parsedDirection;
    }

}
