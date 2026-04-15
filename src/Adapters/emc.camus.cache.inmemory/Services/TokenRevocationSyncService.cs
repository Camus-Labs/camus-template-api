using emc.camus.application.Auth;
using emc.camus.cache.inmemory.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace emc.camus.cache.inmemory.Services;

/// <summary>
/// Background service that periodically synchronizes the in-memory token revocation cache
/// with the persistence layer. Loads all active revoked tokens on startup and refreshes
/// at a configurable interval. Expired entries are naturally excluded by the repository query.
/// </summary>
/// <remarks>
/// Uses <see cref="IServiceScopeFactory"/> to resolve scoped <see cref="IGeneratedTokenRepository"/>
/// from the singleton background service context. If no repository is registered (in-memory
/// persistence mode), the service skips sync cycles.
/// </remarks>
internal sealed partial class TokenRevocationSyncService : BackgroundService
{
    private readonly ITokenRevocationCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly InMemoryCacheSettings _settings;
    private readonly ILogger<TokenRevocationSyncService> _logger;

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Token revocation sync service started with {SyncIntervalSeconds}s interval.")]
    private partial void LogServiceStarted(int syncIntervalSeconds);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Token revocation sync service stopped.")]
    private partial void LogServiceStopped();

    [LoggerMessage(Level = LogLevel.Error,
        Message = "No IGeneratedTokenRepository registered — token revocation sync requires a persistence adapter.")]
    private partial void LogNoRepositoryRegistered();

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Synchronized {Count} active revoked tokens into cache.")]
    private partial void LogSyncCompleted(int count);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Token revocation sync cycle failed.")]
    private partial void LogSyncCycleFailed(Exception exception);

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevocationSyncService"/> class.
    /// </summary>
    /// <param name="cache">The token revocation cache to synchronize.</param>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped dependencies.</param>
    /// <param name="settings">Configuration settings controlling sync interval.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public TokenRevocationSyncService(
        ITokenRevocationCache cache,
        IServiceScopeFactory scopeFactory,
        InMemoryCacheSettings settings,
        ILogger<TokenRevocationSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Executes the synchronization loop: yields to unblock host startup, loads revoked tokens
    /// immediately, then periodically refreshes the cache from the repository using a
    /// <see cref="PeriodicTimer"/> to avoid interval drift. Cancellation disposes the timer,
    /// causing <see cref="PeriodicTimer.WaitForNextTickAsync"/> to return <c>false</c> and
    /// exit the loop naturally.
    /// </summary>
    /// <param name="stoppingToken">Token signaled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        LogServiceStarted(_settings.TokenRevocationCache.SyncIntervalSeconds);

        var baseInterval = TimeSpan.FromSeconds(_settings.TokenRevocationCache.SyncIntervalSeconds);
        using var timer = new PeriodicTimer(baseInterval);
        using var _ = stoppingToken.Register(timer.Dispose);

        await RunSyncCycleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(CancellationToken.None))
        {
            await RunSyncCycleAsync(stoppingToken);
        }

        LogServiceStopped();
    }

    private async Task RunSyncCycleAsync(CancellationToken ct)
    {
        try
        {
            await SyncFromRepositoryAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancellation during a sync cycle is expected during shutdown.
            // The caller's timer-dispose pattern will exit the loop naturally.
        }
        catch (Exception ex)
        {
            LogSyncCycleFailed(ex);
        }
    }

    private async Task SyncFromRepositoryAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetService<IGeneratedTokenRepository>();

        if (repository == null)
        {
            LogNoRepositoryRegistered();
            return;
        }

        var revokedTokens = await repository.GetActiveRevokedJtisAsync(ct);
        _cache.Refresh(revokedTokens);

        LogSyncCompleted(revokedTokens.Count);
    }
}
