using FluentAssertions;
using Moq;
using emc.camus.application.Auth;
using emc.camus.cache.inmemory.Configurations;
using emc.camus.cache.inmemory.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace emc.camus.cache.inmemory.test.Services;

public class TokenRevocationSyncServiceTests
{
    private static readonly Guid RevokedJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly TokenRevocationCache _cache = new();
    private readonly InMemoryCacheSettings _settings = new() { TokenRevocationCache = new() { SyncIntervalSeconds = 10 } };
    private readonly Mock<ILogger<TokenRevocationSyncService>> _loggerMock = new();
    private readonly List<(LogLevel Level, string Message)> _logEntries = [];

    public TokenRevocationSyncServiceTests()
    {
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var level = (LogLevel)invocation.Arguments[0];
                var formatter = invocation.Arguments[4];
                var state = invocation.Arguments[2];
                var exception = invocation.Arguments[3] as Exception;
                var message = formatter.GetType().GetMethod("Invoke")!
                    .Invoke(formatter, [state, exception]) as string ?? "";
                _logEntries.Add((level, message));
            }));
    }

    [Fact]
    public async Task ExecuteAsync_WithRepository_LoadsRevokedTokensIntoCache()
    {
        // Arrange
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { RevokedJti });

        var scopeFactory = CreateScopeFactory(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act — start the service and cancel after the first sync cycle
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));
        await service.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _cache.IsRevoked(RevokedJti).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRepository_LogsError()
    {
        // Arrange — no IGeneratedTokenRepository registered
        var scopeFactory = CreateScopeFactory(repository: null);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));
        await service.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert — cache remains empty, error logged
        _cache.IsRevoked(RevokedJti).Should().BeFalse();
        VerifyLoggedAtLevel(LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryThrows_LogsErrorAndContinues()
    {
        // Arrange — short interval so backoff stays within test window
        var shortIntervalSettings = new InMemoryCacheSettings
        {
            TokenRevocationCache = new() { SyncIntervalSeconds = 10 }
        };
        var callCount = 0;
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Database connection failed.");
                }

                return Task.FromResult(new HashSet<Guid> { RevokedJti });
            });

        var scopeFactory = CreateScopeFactory(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, shortIntervalSettings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act — service should not crash on repository failure
        await service.StartAsync(cts.Token);
        // Wait enough for: initial sync (fails) + backoff (2^1 * 10 = 20s, but capped by cancellation)
        // The service will be waiting in Task.Delay during backoff — cancel to verify it doesn't crash
        await Task.Delay(200, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert — error logged, service did not crash
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("sync") && e.Message.Contains("failed"));
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

        var scopeFactory = CreateScopeFactory(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));
        await service.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);
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

        var scopeFactory = CreateScopeFactory(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, _settings, _loggerMock.Object);

        // Act — service should exit cleanly without logging an error
        await service.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert — no error logged, service stopped gracefully with shutdown info log
        VerifyNeverLoggedAtLevel(LogLevel.Error);
        _logEntries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("stopped"));
    }

    [Fact]
    public async Task ExecuteAsync_PeriodicTick_SyncsOnSubsequentCycles()
    {
        // Arrange — 1s interval so the timer ticks within the test window
        var shortIntervalSettings = new InMemoryCacheSettings
        {
            TokenRevocationCache = new() { SyncIntervalSeconds = 1 }
        };
        var callCount = 0;
        var repositoryMock = new Mock<IGeneratedTokenRepository>();
        repositoryMock
            .Setup(r => r.GetActiveRevokedJtisAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(_ =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(new HashSet<Guid> { RevokedJti });
            });

        var scopeFactory = CreateScopeFactory(repositoryMock.Object);
        var service = new TokenRevocationSyncService(_cache, scopeFactory, shortIntervalSettings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act — let the service run long enough for the initial sync + at least one timer tick
        await service.StartAsync(cts.Token);
        await Task.Delay(1500, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert — repository called at least twice (initial + one periodic tick)
        callCount.Should().BeGreaterThanOrEqualTo(2);
        _cache.IsRevoked(RevokedJti).Should().BeTrue();
    }

    private void VerifyLoggedAtLevel(LogLevel level)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyNeverLoggedAtLevel(LogLevel level)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    private static IServiceScopeFactory CreateScopeFactory(IGeneratedTokenRepository? repository)
    {
        var services = new ServiceCollection();
        if (repository != null)
        {
            services.AddSingleton(repository);
        }
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(() =>
            {
                var scope = serviceProvider.CreateScope();
                return scope;
            });

        return scopeFactoryMock.Object;
    }
}
