namespace emc.camus.application.Common;

/// <summary>
/// Generic base for optional sorting parameters.
/// When both values are null, no sorting is applied. Both field and direction must be specified together.
/// </summary>
/// <typeparam name="TField">The enum type representing the sortable fields.</typeparam>
public record SortParams<TField> where TField : struct, Enum
{
    /// <summary>The field to sort by, or null when no sorting is requested.</summary>
    public TField? Field { get; }

    /// <summary>The direction to sort in, or null when no sorting is requested.</summary>
    public SortDirection? Direction { get; }

    /// <summary>
    /// Creates a new instance from raw string values, performing coercion and validation.
    /// </summary>
    /// <param name="sortBy">The sort field name (case-insensitive), or null.</param>
    /// <param name="sortDirection">The sort direction (case-insensitive), or null.</param>
    /// <exception cref="ArgumentException">Thrown when only one value is provided or values cannot be parsed to a valid enum member.</exception>
    public SortParams(string? sortBy = null, string? sortDirection = null)
    {
        if (sortBy is null && sortDirection is null)
        {
            return;
        }

        ValidateSortInputs(sortBy, sortDirection);

        Field = ParseSortField(sortBy!);
        Direction = ParseSortDirection(sortDirection!);
    }

    private static void ValidateSortInputs(string? sortBy, string? sortDirection)
    {
        if (sortBy is null || sortDirection is null)
        {
            throw new ArgumentException("Both sortBy and sortDirection must be provided together.");
        }
    }

    private static TField ParseSortField(string sortBy)
    {
        if (!Enum.TryParse<TField>(sortBy, ignoreCase: true, out var parsedField))
        {
            var allowed = string.Join(", ", Enum.GetNames<TField>().Select(n => char.ToLowerInvariant(n[0]) + n[1..]));
            throw new ArgumentException($"Invalid value for sortBy: '{sortBy}'. Allowed values: {allowed}.");
        }

        return parsedField;
    }

    private static SortDirection ParseSortDirection(string sortDirection)
    {
        if (!Enum.TryParse<SortDirection>(sortDirection, ignoreCase: true, out var parsedDirection))
        {
            throw new ArgumentException($"Invalid value for sortDirection: '{sortDirection}'. Allowed values: asc, desc.");
        }

        return parsedDirection;
    }
}
