using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using emc.camus.persistence.inmemory.Services;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Services;

public class UnitOfWorkTests
{
    private readonly Mock<ILogger<UnitOfWork>> _loggerMock;
    private readonly ConcurrentBag<(LogLevel Level, string Message)> _entries;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        (_loggerMock, _entries) = LogCaptureBuilder.Create<UnitOfWork>();
        _unitOfWork = new UnitOfWork(_loggerMock.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<UnitOfWork>? logger = null;

        // Act
        var act = () => new UnitOfWork(logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    // --- BeginTransactionAsync ---

    [Fact]
    public async Task BeginTransactionAsync_InMemoryNoOp_LogsSkippedOperation()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        _entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("BeginTransaction") &&
            e.Message.Contains("skipped"));
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_InMemoryNoOp_LogsSkippedOperation()
    {
        // Act
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        _entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("Commit") &&
            e.Message.Contains("skipped"));
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_InMemoryNoOp_LogsSkippedOperation()
    {
        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("Rollback") &&
            e.Message.Contains("skipped"));
    }

    // --- CheckConnectivityAsync ---

    [Fact]
    public async Task CheckConnectivityAsync_InMemoryNoOp_LogsSkippedOperation()
    {
        // Act
        await _unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        _entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("CheckConnectivity") &&
            e.Message.Contains("skipped"));
    }
}
