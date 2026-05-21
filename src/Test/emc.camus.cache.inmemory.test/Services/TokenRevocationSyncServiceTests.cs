using System.Collections.Concurrent;
using FluentAssertions;
using Moq;
using emc.camus.application.Auth;
using emc.camus.cache.inmemory.Configurations;
using emc.camus.cache.inmemory.Services;
using emc.camus.cache.inmemory.test.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.cache.inmemory.test.Services;

public class TokenRevocationSyncServiceTests
{
    private static readonly Guid RevokedJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly TokenRevocationCache _cache = new();
    private readonly InMemoryCacheSettings _settings = new() { TokenRevocationCache = new() { SyncIntervalSeconds = 10 } };
    private readonly Mock<ILogger<TokenRevocationSyncService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly ConcurrentBag<(LogLevel Level, string Message)> _logEntries;

    public TokenRevocationSyncServiceTests()
    {
        (_loggerMock, _logEntries) = LogCaptureBuilder.Create<TokenRevocationSyncService>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRepository_LoadsRevokedTokensIntoCache()
    {
        // Arrange
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { RevokedJti });

        var scopeFactory = ScopeFactoryBuilder.Create(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelper.WaitUntilAsync(() => _cache.IsRevoked(RevokedJti));
        await service.StopAsync(CancellationToken.None);

        // Assert
        _cache.IsRevoked(RevokedJti).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRepository_LogsWarning()
    {
        // Arrange — no IGeneratedTokenRepository registered
        var scopeFactory = ScopeFactoryBuilder.Create(repository: null);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelper.WaitUntilAsync(() => _logEntries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("IGeneratedTokenRepository")));
        await service.StopAsync(CancellationToken.None);

        // Assert — cache remains empty, warning logged
        _cache.IsRevoked(RevokedJti).Should().BeFalse();
        _logEntries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("IGeneratedTokenRepository"));
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryThrows_LogsWarningAndContinues()
    {
        // Arrange
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .SetupSequence(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed."))
            .ReturnsAsync(new HashSet<Guid> { RevokedJti });

        var scopeFactory = ScopeFactoryBuilder.Create(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelper.WaitUntilAsync(() => _logEntries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("revocation") && e.Message.Contains("failed")));
        await service.StopAsync(CancellationToken.None);

        // Assert — warning logged, service did not crash
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains("revocation") && e.Message.Contains("sync") && e.Message.Contains("failed"));
    }

    [Fact]
    public async Task ExecuteAsync_SyncReplacesExistingCacheEntries()
    {
        // Arrange — cache has an old entry, repository returns a different set
        var oldJti = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _cache.Revoke(oldJti);

        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { RevokedJti });

        var scopeFactory = ScopeFactoryBuilder.Create(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelper.WaitUntilAsync(() => _cache.IsRevoked(RevokedJti));
        await service.StopAsync(CancellationToken.None);

        // Assert — old entry replaced, new entry present
        _cache.IsRevoked(oldJti).Should().BeFalse();
        _cache.IsRevoked(RevokedJti).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CancellationDuringSync_StopsGracefully()
    {
        // Arrange — repository throws OperationCanceledException when token is canceled
        using var cts = new CancellationTokenSource();
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new HashSet<Guid>());
            });

        var scopeFactory = ScopeFactoryBuilder.Create(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await AsyncWaitHelper.WaitUntilAsync(() => _logEntries.Any(e => e.Level == LogLevel.Information && e.Message.Contains("revocation") && e.Message.Contains("stopped")));
        await service.StopAsync(CancellationToken.None);

        // Assert — no error logged, service stopped gracefully with shutdown info log
        _logEntries.Should().NotContain(e => e.Level == LogLevel.Error);
        _logEntries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("revocation") && e.Message.Contains("stopped"));
    }

    [Fact]
    public async Task ExecuteAsync_PeriodicTick_SyncsOnSubsequentCycles()
    {
        // Arrange
        var callCount = 0;
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(new HashSet<Guid> { RevokedJti });
            });

        var scopeFactory = ScopeFactoryBuilder.Create(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _timeProvider, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await AsyncWaitHelper.WaitUntilAsync(() => callCount >= 1);
        await AsyncWaitHelper.WaitUntilAsync(() =>
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
            return callCount >= 2;
        });
        await service.StopAsync(CancellationToken.None);

        // Assert — repository called at least twice (initial + one periodic tick)
        callCount.Should().BeGreaterThanOrEqualTo(2);
        _cache.IsRevoked(RevokedJti).Should().BeTrue();
    }

}
