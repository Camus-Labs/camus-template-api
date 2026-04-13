using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// PostgreSQL implementation of API info data access using Dapper.
/// Contains only raw SQL execution with no branching or business logic.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ApiInfoDataAccess : IApiInfoDataAccess
{
    /// <inheritdoc />
    public async Task<bool> CheckTableExistsAsync(IDbConnection connection, CancellationToken ct = default)
    {
        const string checkTableSql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_schema = 'camus'
                AND table_name = 'api_info'
            )";

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(checkTableSql, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<ApiInfoModel?> FindByVersionAsync(IDbConnection connection, string version, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                name,
                version,
                status,
                features
            FROM camus.api_info
            WHERE version = @Version";

        return await connection.QuerySingleOrDefaultAsync<ApiInfoModel>(
            new CommandDefinition(sql, new { version }, cancellationToken: ct));
    }
}
