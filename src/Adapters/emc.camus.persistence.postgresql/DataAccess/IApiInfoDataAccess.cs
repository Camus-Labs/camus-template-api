using System.Data;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// Thin data access layer for API info SQL operations.
/// </summary>
internal interface IApiInfoDataAccess
{
    /// <summary>
    /// Checks whether the api_info table exists in the database schema.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if the table exists; otherwise false.</returns>
    Task<bool> CheckTableExistsAsync(IDbConnection connection, CancellationToken ct = default);

    /// <summary>
    /// Finds API info by version.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="version">The API version to look up.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The API info model if found; otherwise null.</returns>
    Task<ApiInfoModel?> FindByVersionAsync(IDbConnection connection, string version, CancellationToken ct = default);
}
