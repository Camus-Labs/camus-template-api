using System.Data;
using Dapper;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using Npgsql;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// Repository for managing generated tokens in PostgreSQL.
/// Provides entity-centric methods to persist and retrieve custom generated tokens with permissions.
/// </summary>
public class PSGeneratedTokenRepository : IGeneratedTokenRepository
{

    /// <inheritdoc/>
    public async Task CreateAsync(IDbConnection connection, GeneratedToken generatedToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(generatedToken);

        const string sql = @"
            INSERT INTO camus.generated_tokens (
                jti, creator_user_id, creator_username, token_username, 
                permissions, expires_on, is_revoked
            )
            VALUES (
                @Jti, @CreatorUserId, @CreatorUsername, @TokenUsername, 
                @Permissions, @ExpiresOn, @IsRevoked
            )";

        await connection.ExecuteAsync(sql, new
        {
            generatedToken.Jti,
            generatedToken.CreatorUserId,
            generatedToken.CreatorUsername,
            generatedToken.TokenUsername,
            Permissions = generatedToken.Permissions.ToArray(),
            generatedToken.ExpiresOn,
            generatedToken.IsRevoked
        });
    }

    /// <inheritdoc/>
    public async Task<GeneratedToken?> GetByJtiAsync(IDbConnection connection, Guid jti)
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = @"
            SELECT 
                jti, creator_user_id, creator_username, token_username, 
                permissions, expires_on, created_at, is_revoked, revoked_at
            FROM camus.generated_tokens
            WHERE jti = @Jti";

        var result = await connection.QueryFirstOrDefaultAsync<GeneratedTokenModel>(sql, new { jti });

        if (result == null)
        {
            return null;
        }

        return result.ToEntity();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<GeneratedToken>> GetPagedByCreatorUserIdAsync(IDbConnection connection, Guid creatorUserId, PaginationParams pagination, GeneratedTokenFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pagination);

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

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (totalCount == 0)
        {
            return new PagedResult<GeneratedToken>([], 0, pagination.Page, pagination.PageSize);
        }

        var results = await connection.QueryAsync<GeneratedTokenModel>(dataSql, parameters);

        var items = results.Select(r => r.ToEntity()).ToList();

        return new PagedResult<GeneratedToken>(items, totalCount, pagination.Page, pagination.PageSize);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(IDbConnection connection, GeneratedToken generatedToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(generatedToken);

        const string sql = @"
            UPDATE camus.generated_tokens
            SET is_revoked = @IsRevoked, revoked_at = @RevokedAt
            WHERE jti = @Jti";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            generatedToken.Jti,
            generatedToken.IsRevoked,
            generatedToken.RevokedAt
        });

        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"Generated token with JTI '{generatedToken.Jti}' not found.");
        }
    }
}
