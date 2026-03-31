using emc.camus.application.ApiInfo;
using emc.camus.domain.Auth;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of API info repository that loads configuration from settings.
/// </summary>
internal sealed class IMApiInfoRepository : IApiInfoRepository
{
    private readonly InMemoryModelSettings _settings;
    private Dictionary<string, ApiInfo> _apiInfoByVersion = new();
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="IMApiInfoRepository"/> class.
    /// </summary>
    /// <param name="settings">In-memory model settings containing API info definitions.</param>
    public IMApiInfoRepository(
        InMemoryModelSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>
    /// Initializes the in-memory repository by loading API info from configuration settings.
    /// This method must be called once at application startup to populate the in-memory store.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when API info configuration is invalid.
    /// </exception>
    public Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("IMApiInfoRepository already initialized.");
        }

        _apiInfoByVersion = new Dictionary<string, ApiInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var config in _settings.ApiInfos)
        {
            var apiInfo = new ApiInfo(
                version: config.Version,
                status: config.Status,
                features: config.Features?.Count > 0 ? config.Features : null,
                name: !string.IsNullOrWhiteSpace(config.Name) ? config.Name : null
            );

            _apiInfoByVersion[config.Version] = apiInfo;
        }

        _initialized = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets API information by version.
    /// </summary>
    /// <param name="version">The API version to retrieve.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An ApiInfo object if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified version is not found.
    /// </exception>
    public Task<ApiInfo> GetByVersionAsync(string version, CancellationToken ct = default)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (!_apiInfoByVersion.TryGetValue(version, out var apiInfo))
        {
            throw new KeyNotFoundException($"API info not found for version '{version}'.");
        }

        return Task.FromResult(apiInfo);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync() first.");
        }
    }
}
