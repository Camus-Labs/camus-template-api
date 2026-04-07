using System.Data;
using Dapper;
using emc.camus.application.ApiInfo;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Services;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of API info repository using Dapper.
/// </summary>
internal sealed class PSApiInfoRepository : IApiInfoRepository
{
    private static bool s_initialized;
    private readonly PSUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSApiInfoRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    public PSApiInfoRepository(
        PSUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Initializes the PostgreSQL repository by validating the database connection and schema.
    /// This method must be called once at application startup to verify database connectivity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (s_initialized)
        {
            throw new InvalidOperationException("PSApiInfoRepository already initialized.");
        }

        // Test connection and verify table exists
        var connection = await _unitOfWork.GetConnectionAsync(ct);

        const string checkTableSql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_schema = 'camus'
                AND table_name = 'api_info'
            )";

        var tableExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(checkTableSql, cancellationToken: ct));

        if (!tableExists)
        {
            throw new InvalidOperationException(
                "Required table 'api_info' does not exist in the database. " +
                "Please run database migrations to create the schema.");
        }

        s_initialized = true;
    }

    /// <summary>
    /// Gets API information by version from the database.
    /// </summary>
    /// <param name="version">The API version to retrieve.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An ApiInfo object if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when version is null or empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified version is not found.
    /// </exception>
    public async Task<ApiInfo> GetByVersionAsync(string version, CancellationToken ct = default)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        const string sql = @"
            SELECT
                name,
                version,
                status,
                features
            FROM camus.api_info
            WHERE version = @Version";

        var result = await connection.QuerySingleOrDefaultAsync<ApiInfoModel>(
            new CommandDefinition(sql, new { version }, cancellationToken: ct));

        if (result == null)
        {
            throw new KeyNotFoundException($"API info not found for version '{version}'.");
        }

        return result.ToEntity();
    }

    private static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync() first.");
        }
    }
}
