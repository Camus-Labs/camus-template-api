using System.Data;
using Dapper;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of API info repository using Dapper.
/// </summary>
internal sealed class PSApiInfoRepository : IApiInfoRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSApiInfoRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    public PSApiInfoRepository(
        IConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Initializes the PostgreSQL repository by validating the database connection and schema.
    /// This method must be called once at application startup to verify database connectivity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public void Initialize()
    {
        if (_initialized)
        {
            throw new InvalidOperationException("PSApiInfoRepository already initialized.");
        }

        // Test connection and verify table exists
        using var connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();

        const string checkTableSql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_schema = 'camus'
                AND table_name = 'api_info'
            )";

        var tableExists = connection.ExecuteScalar<bool>(checkTableSql);

        if (!tableExists)
        {
            throw new InvalidOperationException(
                "Required table 'api_info' does not exist in the database. " +
                "Please run database migrations to create the schema.");
        }

        _initialized = true;
    }

    /// <summary>
    /// Gets API information by version from the database.
    /// </summary>
    /// <param name="version">The API version to retrieve.</param>
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
    public async Task<ApiInfo> GetByVersionAsync(string version)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        using var connection = await _connectionFactory.CreateConnectionAsync();

        const string sql = @"
            SELECT
                name,
                version,
                status,
                features
            FROM camus.api_info
            WHERE version = @Version";

        var result = await connection.QuerySingleOrDefaultAsync<ApiInfoModel>(
            sql,
            new { version });

        if (result == null)
        {
            throw new KeyNotFoundException($"API info not found for version '{version}'.");
        }

        return result.ToEntity();
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
