using emc.camus.application.ApiInfo;
using emc.camus.application.Configurations;
using emc.camus.domain.Auth;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of API info repository that loads configuration from settings.
/// </summary>
public class IMApiInfoRepository : IApiInfoRepository
{
    private readonly InMemoryAppDataSettings _settings;
    private Dictionary<string, ApiInfo> _apiInfoByVersion = new();
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="IMApiInfoRepository"/> class.
    /// </summary>
    /// <param name="settings">Application data settings containing API info definitions.</param>
    public IMApiInfoRepository(
        AppDataSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.InMemory;
    }

    /// <summary>
    /// Initializes the in-memory repository by loading API info from configuration settings.
    /// This method must be called once at application startup to populate the in-memory store.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when API info configuration is invalid.
    /// </exception>
    public void Initialize()
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
    }

    /// <summary>
    /// Gets API information by version.
    /// </summary>
    /// <param name="version">The API version to retrieve.</param>
    /// <returns>An ApiInfo object if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified version is not found.
    /// </exception>
    public Task<ApiInfo> GetByVersionAsync(string version)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (!_apiInfoByVersion.TryGetValue(version, out var apiInfo))
        {
            throw new KeyNotFoundException($"API info not found for version '{version}'.");
        }

        return Task.FromResult(apiInfo);
    }

    /// <summary>
    /// Gets all available API versions.
    /// </summary>
    /// <returns>A list of all ApiInfo objects.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    public Task<List<ApiInfo>> GetAllAsync()
    {
        EnsureInitialized();

        return Task.FromResult(_apiInfoByVersion.Values.ToList());
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
