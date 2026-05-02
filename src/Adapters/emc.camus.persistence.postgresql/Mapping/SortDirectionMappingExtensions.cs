using emc.camus.application.Common;

namespace emc.camus.persistence.postgresql.Mapping;

/// <summary>
/// Maps <see cref="SortDirection"/> values to SQL ORDER BY keywords.
/// </summary>
internal static class SortDirectionMappingExtensions
{
    /// <summary>
    /// Returns the SQL ORDER BY keyword for the sort direction.
    /// </summary>
    /// <param name="direction">The sort direction.</param>
    /// <returns>"ASC" or "DESC".</returns>
    public static string ToSql(this SortDirection direction) =>
        direction == SortDirection.Asc ? "ASC" : "DESC";
}
