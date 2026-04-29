namespace emc.camus.application.Common;

/// <summary>
/// Specifies the direction of sorting.
/// </summary>
public enum SortDirection
{
    /// <summary>Ascending order.</summary>
    Asc,

    /// <summary>Descending order.</summary>
    Desc
}

/// <summary>
/// Extension methods for <see cref="SortDirection"/>.
/// </summary>
public static class SortDirectionExtensions
{
    /// <summary>
    /// Returns the SQL ORDER BY keyword for the sort direction.
    /// </summary>
    /// <param name="direction">The sort direction.</param>
    /// <returns>"ASC" or "DESC".</returns>
    public static string ToSql(this SortDirection direction) =>
        direction == SortDirection.Asc ? "ASC" : "DESC";
}
