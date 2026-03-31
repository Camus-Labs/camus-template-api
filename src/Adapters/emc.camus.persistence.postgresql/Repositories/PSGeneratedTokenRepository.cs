using Dapper;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing generated tokens in PostgreSQL.
/// Provides entity-centric methods to persist and retrieve custom generated tokens with permissions.
/// </summary>
internal sealed class PSGeneratedTokenRepository : IGeneratedTokenRepository
{
    private readonly PSUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSGeneratedTokenRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    public PSGeneratedTokenRepository(PSUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a new generated token record in the database.
    /// Validates that the creator user exists before inserting.
    /// </summary>
    /// <param name="generatedToken">The generated token domain entity.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when generatedToken is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a token with the same JTI already exists.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the creator user does not exist.</exception>
    public async Task CreateAsync(GeneratedToken generatedToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(generatedToken);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        const string sql = @"
            INSERT INTO camus.generated_tokens (
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, is_revoked
            )
            VALUES (
                @Jti, @CreatorUserId, @CreatorUsername, @TokenUsername,
                @Permissions, @ExpiresOn, @IsRevoked
            )";

        const string jtiCheckSql = "SELECT EXISTS (SELECT 1 FROM camus.generated_tokens WHERE jti = @Jti)";
        var jtiExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(jtiCheckSql, new { generatedToken.Jti }, cancellationToken: ct));

        if (jtiExists)
        {
            throw new InvalidOperationException($"A generated token with JTI '{generatedToken.Jti}' already exists.");
        }

        const string fkCheckSql = "SELECT EXISTS (SELECT 1 FROM camus.users WHERE id = @CreatorUserId)";
        var creatorExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(fkCheckSql, new { generatedToken.CreatorUserId }, cancellationToken: ct));

        if (!creatorExists)
        {
            throw new KeyNotFoundException($"Creator user with ID '{generatedToken.CreatorUserId}' not found.");
        }

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                generatedToken.Jti,
                generatedToken.CreatorUserId,
                generatedToken.CreatorUsername,
                generatedToken.TokenUsername,
                Permissions = generatedToken.Permissions.ToArray(),
                generatedToken.ExpiresOn,
                generatedToken.IsRevoked
            }, cancellationToken: ct));
    }

    /// <summary>
    /// Retrieves a generated token from PostgreSQL by its JTI (JWT ID).
    /// </summary>
    /// <param name="jti">The JWT ID to search for.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The generated token if found, otherwise <see langword="null"/>.</returns>
    public async Task<GeneratedToken?> GetByJtiAsync(Guid jti, CancellationToken ct = default)
    {
        var connection = await _unitOfWork.GetConnectionAsync(ct);

        const string sql = @"
            SELECT
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, created_at, is_revoked, revoked_at
            FROM camus.generated_tokens
            WHERE jti = @Jti";

        var result = await connection.QueryFirstOrDefaultAsync<GeneratedTokenModel>(
            new CommandDefinition(sql, new { jti }, cancellationToken: ct));

        if (result == null)
        {
            return null;
        }

        return result.ToEntity();
    }

    /// <summary>
    /// Retrieves a paged list of generated tokens from PostgreSQL for a specific creator user.
    /// Supports optional filtering by revocation and expiration status.
    /// </summary>
    /// <param name="creatorUserId">The user ID of the creator.</param>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="filter">Optional filter criteria for excluding revoked or expired tokens.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A paged result containing the matching tokens and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagination"/> is null.</exception>
    public async Task<PagedResult<GeneratedToken>> GetPagedByCreatorUserIdAsync(Guid creatorUserId, PaginationParams pagination, GeneratedTokenFilter? filter = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var whereClause = "WHERE creator_user_id = @CreatorUserId";

        if (filter?.ExcludeRevoked == true)
        {
            whereClause += " AND is_revoked = false";
        }

        if (filter?.ExcludeExpired == true)
        {
            whereClause += " AND expires_on > @Now";
        }

        var countSql = $@"
            SELECT COUNT(*)
            FROM camus.generated_tokens
            {whereClause}";

        var dataSql = $@"
            SELECT
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, created_at, is_revoked, revoked_at
            FROM camus.generated_tokens
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var parameters = new DynamicParameters();
        parameters.Add("CreatorUserId", creatorUserId);
        parameters.Add("PageSize", pagination.PageSize);
        parameters.Add("Offset", pagination.Offset);
        parameters.Add("Now", DateTime.UtcNow);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        if (totalCount == 0)
        {
            return new PagedResult<GeneratedToken>([], 0, pagination.Page, pagination.PageSize);
        }

        var results = await connection.QueryAsync<GeneratedTokenModel>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct));

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

        const string sql = @"
            UPDATE camus.generated_tokens
            SET is_revoked = @IsRevoked, revoked_at = @RevokedAt
            WHERE jti = @Jti";

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                generatedToken.Jti,
                generatedToken.IsRevoked,
                generatedToken.RevokedAt
            }, cancellationToken: ct));

        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"Generated token with JTI '{generatedToken.Jti}' not found.");
        }
    }
}
