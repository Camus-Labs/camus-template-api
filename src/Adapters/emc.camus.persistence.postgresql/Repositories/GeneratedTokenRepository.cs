using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Services;
using static emc.camus.persistence.postgresql.Services.QueryExecutionGuard;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing generated tokens in PostgreSQL.
/// Delegates raw SQL execution to <see cref="IGeneratedTokenDataAccess"/>.
/// </summary>
internal sealed class GeneratedTokenRepository : IGeneratedTokenRepository
{
    /// <summary>
    /// Maps <see cref="GeneratedTokenSortField"/> values to safe, allow-listed database column names.
    /// </summary>
    private static readonly Dictionary<GeneratedTokenSortField, string> SortFieldColumnMap =
        new Dictionary<GeneratedTokenSortField, string>
        {
            { GeneratedTokenSortField.TokenUsername, "token_username" },
            { GeneratedTokenSortField.ExpiresOn, "expires_on" },
            { GeneratedTokenSortField.CreatedAt, "created_at" },
            { GeneratedTokenSortField.RevokedAt, "revoked_at" }
        };

    private readonly UnitOfWork _unitOfWork;
    private readonly IGeneratedTokenDataAccess _dataAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedTokenRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="dataAccess">Data access layer for raw SQL execution.</param>
    public GeneratedTokenRepository(UnitOfWork unitOfWork, IGeneratedTokenDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dataAccess);

        _unitOfWork = unitOfWork;
        _dataAccess = dataAccess;
    }

    /// <summary>
    /// Creates a new generated token record in the database.
    /// Validates that the creator user exists before inserting.
    /// </summary>
    /// <param name="generatedToken">The generated token domain entity.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when generatedToken is null.</exception>
    /// <exception cref="DataConflictException">Thrown when a token with the same JTI or token username already exists, or when the creator user does not exist.</exception>
    public async Task CreateAsync(GeneratedToken generatedToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(generatedToken);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var jtiExists = await ExecuteAsync(
            () => _dataAccess.JtiExistsAsync(connection, generatedToken.Jti, ct),
            nameof(_dataAccess.JtiExistsAsync));
        if (jtiExists)
        {
            throw new DataConflictException($"A generated token with JTI '{generatedToken.Jti}' already exists.");
        }

        var usernameExists = await ExecuteAsync(
            () => _dataAccess.TokenUsernameExistsAsync(connection, generatedToken.TokenUsername, ct),
            nameof(_dataAccess.TokenUsernameExistsAsync));
        if (usernameExists)
        {
            throw new DataConflictException($"A generated token with username '{generatedToken.TokenUsername}' already exists.");
        }

        var creatorExists = await ExecuteAsync(
            () => _dataAccess.CreatorUserExistsAsync(connection, generatedToken.CreatorUserId, ct),
            nameof(_dataAccess.CreatorUserExistsAsync));
        if (!creatorExists)
        {
            throw new DataConflictException($"Cannot create generated token because creator user '{generatedToken.CreatorUserId}' does not exist.");
        }

        await ExecuteAsync(
            () => _dataAccess.InsertAsync(
                connection,
                generatedToken.Jti,
                generatedToken.CreatorUserId,
                generatedToken.CreatorUsername,
                generatedToken.TokenUsername,
                generatedToken.Permissions.ToArray(),
                generatedToken.ExpiresOn,
                generatedToken.IsRevoked,
                ct),
            nameof(_dataAccess.InsertAsync));
    }

    /// <summary>
    /// Retrieves a generated token from PostgreSQL by its JTI (JWT ID).
    /// </summary>
    /// <param name="jti">The JWT ID to search for.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The generated token if found, otherwise <see langword="null"/>.</returns>
    public async Task<GeneratedToken?> GetByJtiAsync(Guid jti, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);

        var connection = await _unitOfWork.GetConnectionAsync(ct);
        var result = await ExecuteAsync(
            () => _dataAccess.FindByJtiAsync(connection, jti, ct),
            nameof(_dataAccess.FindByJtiAsync));

        return result?.ToEntity();
    }

    /// <summary>
    /// Retrieves a paged list of generated tokens from PostgreSQL for a specific creator user.
    /// Supports optional filtering by revocation and expiration status.
    /// </summary>
    /// <param name="creatorUserId">The user ID of the creator.</param>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="filter">Optional filter criteria for excluding revoked or expired tokens.</param>
    /// <param name="sort">Optional sort parameters for ordering results.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A paged result containing the matching tokens and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagination"/> is null.</exception>
    public async Task<PagedResult<GeneratedToken>> GetPagedByCreatorUserIdAsync(
        Guid creatorUserId,
        PaginationParams pagination,
        GeneratedTokenFilter filter,
        SortParams<GeneratedTokenSortField> sort,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(creatorUserId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(pagination);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(sort);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var excludeRevoked = filter.ExcludeRevoked;
        var excludeExpired = filter.ExcludeExpired;

        var totalCount = await ExecuteAsync(
            () => _dataAccess.CountByCreatorUserIdAsync(
                connection, creatorUserId, excludeRevoked, excludeExpired, ct),
            nameof(_dataAccess.CountByCreatorUserIdAsync));

        if (totalCount == 0)
        {
            return new PagedResult<GeneratedToken>([], 0, pagination.Page, pagination.PageSize);
        }

        string? sortColumn = sort.Field is not null ? SortFieldColumnMap[sort.Field.Value] : null;
        string? sortDirection = sort.Direction?.ToSql();

        var results = await ExecuteAsync(
            () => _dataAccess.GetPageByCreatorUserIdAsync(
                connection, creatorUserId, excludeRevoked, excludeExpired, pagination.PageSize, pagination.Offset, sortColumn, sortDirection, ct),
            nameof(_dataAccess.GetPageByCreatorUserIdAsync));

        var items = results.Select(r => r.ToEntity()).ToList();

        return new PagedResult<GeneratedToken>(items, totalCount, pagination.Page, pagination.PageSize);
    }

    /// <summary>
    /// Persists the current state of a generated token to PostgreSQL (e.g., after revocation).
    /// Updates the revocation status and timestamp.
    /// </summary>
    /// <param name="generatedToken">The generated token domain entity with updated state.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedToken"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no token with the specified JTI exists.</exception>
    public async Task SaveAsync(GeneratedToken generatedToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(generatedToken);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var rowsAffected = await ExecuteAsync(
            () => _dataAccess.UpdateRevocationAsync(
                connection, generatedToken.Jti, generatedToken.IsRevoked, generatedToken.RevokedAt, ct),
            nameof(_dataAccess.UpdateRevocationAsync));

        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"Generated token with JTI '{generatedToken.Jti}' not found.");
        }
    }

    /// <summary>
    /// Retrieves all revoked tokens that have not yet expired from PostgreSQL.
    /// Returns a lightweight projection (JTI only) for cache synchronization.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A set of JTIs for revoked tokens that have not yet expired.</returns>
    public async Task<HashSet<Guid>> GetActiveRevokedJtisAsync(CancellationToken ct = default)
    {
        var connection = await _unitOfWork.GetConnectionAsync(ct);
        var results = await ExecuteAsync(
            () => _dataAccess.GetActiveRevokedJtisAsync(connection, ct),
            nameof(_dataAccess.GetActiveRevokedJtisAsync));

        return results.ToHashSet();
    }
}
