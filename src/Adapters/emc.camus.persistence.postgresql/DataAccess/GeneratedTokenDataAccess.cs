using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// PostgreSQL implementation of generated token data access using Dapper.
/// Contains only raw SQL execution with no branching or business logic.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class GeneratedTokenDataAccess : IGeneratedTokenDataAccess
{
    /// <inheritdoc />
    public async Task<bool> JtiExistsAsync(IDbConnection connection, Guid jti, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM camus.generated_tokens WHERE jti = @Jti)";
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { jti }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> TokenUsernameExistsAsync(IDbConnection connection, string tokenUsername, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM camus.generated_tokens WHERE token_username = @TokenUsername)";
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { tokenUsername }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> CreatorUserExistsAsync(IDbConnection connection, Guid userId, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM camus.users WHERE id = @CreatorUserId)";
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { CreatorUserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task InsertAsync(IDbConnection connection, Guid jti, Guid creatorUserId, string creatorUsername, string tokenUsername, string[] permissions, DateTime expiresOn, bool isRevoked, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO camus.generated_tokens (
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, is_revoked
            )
            VALUES (
                @Jti, @CreatorUserId, @CreatorUsername, @TokenUsername,
                @Permissions, @ExpiresOn, @IsRevoked
            )";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Jti = jti,
                CreatorUserId = creatorUserId,
                CreatorUsername = creatorUsername,
                TokenUsername = tokenUsername,
                Permissions = permissions,
                ExpiresOn = expiresOn,
                IsRevoked = isRevoked
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<GeneratedTokenModel?> FindByJtiAsync(IDbConnection connection, Guid jti, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, created_at, is_revoked, revoked_at
            FROM camus.generated_tokens
            WHERE jti = @Jti";

        return await connection.QueryFirstOrDefaultAsync<GeneratedTokenModel>(
            new CommandDefinition(sql, new { jti }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> CountByCreatorUserIdAsync(IDbConnection connection, Guid creatorUserId, bool excludeRevoked, bool excludeExpired, CancellationToken ct = default)
    {
        var whereClause = "WHERE creator_user_id = @CreatorUserId";
        if (excludeRevoked) whereClause += " AND is_revoked = false";
        if (excludeExpired) whereClause += " AND expires_on > @Now";

        var sql = $"SELECT COUNT(*) FROM camus.generated_tokens {whereClause}";

        var parameters = new DynamicParameters();
        parameters.Add("CreatorUserId", creatorUserId);
        parameters.Add("Now", DateTime.UtcNow);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GeneratedTokenModel>> GetPageByCreatorUserIdAsync(IDbConnection connection, Guid creatorUserId, bool excludeRevoked, bool excludeExpired, int pageSize, int offset, CancellationToken ct = default)
    {
        var whereClause = "WHERE creator_user_id = @CreatorUserId";
        if (excludeRevoked) whereClause += " AND is_revoked = false";
        if (excludeExpired) whereClause += " AND expires_on > @Now";

        var sql = $@"
            SELECT
                jti, creator_user_id, creator_username, token_username,
                permissions, expires_on, created_at, is_revoked, revoked_at
            FROM camus.generated_tokens
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var parameters = new DynamicParameters();
        parameters.Add("CreatorUserId", creatorUserId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);
        parameters.Add("Now", DateTime.UtcNow);

        return await connection.QueryAsync<GeneratedTokenModel>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> UpdateRevocationAsync(IDbConnection connection, Guid jti, bool isRevoked, DateTime? revokedAt, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE camus.generated_tokens
            SET is_revoked = @IsRevoked, revoked_at = @RevokedAt
            WHERE jti = @Jti";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Jti = jti, IsRevoked = isRevoked, RevokedAt = revokedAt }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> GetActiveRevokedJtisAsync(IDbConnection connection, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT jti
            FROM camus.generated_tokens
            WHERE is_revoked = true AND expires_on > @Now";

        return await connection.QueryAsync<Guid>(
            new CommandDefinition(sql, new { Now = DateTime.UtcNow }, cancellationToken: ct));
    }
}
