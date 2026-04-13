using emc.camus.application.ApiInfo;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of API info repository.
/// Delegates raw SQL execution to <see cref="IApiInfoDataAccess"/>.
/// </summary>
internal sealed class ApiInfoRepository : IApiInfoRepository
{
    private readonly UnitOfWork _unitOfWork;
    private readonly InitializationState _initState;
    private readonly IApiInfoDataAccess _dataAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiInfoRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="initState">Container-scoped initialization state shared across scoped instances.</param>
    /// <param name="dataAccess">Data access layer for raw SQL execution.</param>
    public ApiInfoRepository(
        UnitOfWork unitOfWork,
        InitializationState initState,
        IApiInfoDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(initState);
        ArgumentNullException.ThrowIfNull(dataAccess);

        _unitOfWork = unitOfWork;
        _initState = initState;
        _dataAccess = dataAccess;
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
        if (_initState.ApiInfoRepositoryInitialized)
        {
            throw new InvalidOperationException("ApiInfoRepository already initialized.");
        }

        var connection = await _unitOfWork.GetConnectionAsync(ct);
        var tableExists = await _dataAccess.CheckTableExistsAsync(connection, ct);

        if (!tableExists)
        {
            throw new InvalidOperationException(
                "Required table 'api_info' does not exist in the database. " +
                "Please run database migrations to create the schema.");
        }

        _initState.ApiInfoRepositoryInitialized = true;
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

        var result = await _dataAccess.FindByVersionAsync(connection, version, ct)
            ?? throw new KeyNotFoundException($"API info not found for version '{version}'.");

        return result.ToEntity();
    }

    private void EnsureInitialized()
    {
        if (!_initState.ApiInfoRepositoryInitialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync() first.");
        }
    }
}
