using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// PostgreSQL implementation of action audit data access using Dapper.
/// Contains only raw SQL execution with no branching or business logic.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ActionAuditDataAccess : IActionAuditDataAccess
{
    /// <inheritdoc />
    public async Task<bool> UserExistsAsync(IDbConnection connection, Guid userId, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM camus.users WHERE id = @UserId)";
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> InsertAsync(IDbConnection connection, Guid userId, string username, string? traceId, string actionTitle, string actionSummary, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO camus.action_audit (user_id, user_name, trace_id, action_title, action_summary)
            VALUES (@UserId, @Username, @TraceId, @ActionTitle, @ActionSummary)
            RETURNING id";

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                userId,
                username,
                traceId,
                actionTitle,
                actionSummary
            }, cancellationToken: ct));
    }
}
