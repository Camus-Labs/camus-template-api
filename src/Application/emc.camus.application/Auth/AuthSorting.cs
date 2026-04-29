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
/// Both field and direction must be specified together.
/// </summary>
public sealed record GeneratedTokenSortParams
{
    /// <summary>The field to sort by.</summary>
    public GeneratedTokenSortField Field { get; }

    /// <summary>The direction to sort in.</summary>
    public SortDirection Direction { get; }

    /// <summary>
    /// Creates a new instance of <see cref="GeneratedTokenSortParams"/>.
    /// </summary>
    /// <param name="field">The field to sort by.</param>
    /// <param name="direction">The direction to sort in.</param>
    public GeneratedTokenSortParams(GeneratedTokenSortField field, SortDirection direction)
    {
        Field = field;
        Direction = direction;
    }
}
