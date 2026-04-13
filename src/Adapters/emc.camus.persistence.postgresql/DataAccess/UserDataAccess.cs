using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// PostgreSQL implementation of user data access using Dapper.
/// Contains only raw SQL execution with no branching or business logic.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class UserDataAccess : IUserDataAccess
{
    /// <inheritdoc />
    public async Task<IDictionary<string, bool>> CheckRequiredTablesAsync(
        IDbConnection connection, CancellationToken ct = default)
    {
        const string checkTablesSql = @"
            SELECT
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'users'
                )) as users_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'roles'
                )) as roles_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'user_roles'
                )) as user_roles_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'role_permissions'
                )) as role_permissions_exists";

        var result = await connection.QuerySingleAsync<dynamic>(
            new CommandDefinition(checkTablesSql, cancellationToken: ct));

        var resultDict = (IDictionary<string, object>)result;

        return new Dictionary<string, bool>
        {
            ["users"] = resultDict["users_exists"] is true,
            ["roles"] = resultDict["roles_exists"] is true,
            ["user_roles"] = resultDict["user_roles_exists"] is true,
            ["role_permissions"] = resultDict["role_permissions_exists"] is true,
        };
    }

    /// <inheritdoc />
    public async Task<UserModel?> FindByUsernameWithHashAsync(
        IDbConnection connection, string username, CancellationToken ct = default)
    {
        const string userSql = @"
            SELECT
                id,
                username,
                password_hash
            FROM camus.users
            WHERE username = @Username";

        return await connection.QuerySingleOrDefaultAsync<UserModel>(
            new CommandDefinition(userSql, new { username }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<UserModel?> FindByIdAsync(
        IDbConnection connection, Guid userId, CancellationToken ct = default)
    {
        const string userSql = @"
            SELECT id, username
            FROM camus.users
            WHERE id = @UserId";

        return await connection.QuerySingleOrDefaultAsync<UserModel>(
            new CommandDefinition(userSql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleModel>> GetRolesByUserIdAsync(
        IDbConnection connection, Guid userId, CancellationToken ct = default)
    {
        const string rolesSql = @"
            SELECT
                r.id,
                r.name,
                r.description,
                ARRAY_AGG(rp.permission) FILTER (WHERE rp.permission IS NOT NULL) as permissions
            FROM camus.roles r
            INNER JOIN camus.user_roles ur ON r.id = ur.role_id
            LEFT JOIN camus.role_permissions rp ON r.id = rp.role_id
            WHERE ur.user_id = @UserId
            GROUP BY r.id, r.name, r.description
            ORDER BY r.name";

        return await connection.QueryAsync<RoleModel>(
            new CommandDefinition(rolesSql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> UpdateLastLoginAsync(
        IDbConnection connection, Guid userId, CancellationToken ct = default)
    {
        const string updateSql = @"
            UPDATE camus.users
            SET last_login = NOW()
            WHERE id = @UserId";

        return await connection.ExecuteAsync(
            new CommandDefinition(updateSql, new { userId }, cancellationToken: ct));
    }
}
