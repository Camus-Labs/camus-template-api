using FluentAssertions;
using Microsoft.Extensions.Logging;
using emc.camus.persistence.inmemory.Services;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Services;

public class UnitOfWorkTests
{
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var (mock, _) = LogCaptureBuilder.Create<UnitOfWork>();
        _mockLogger = mock;
        _unitOfWork = new UnitOfWork(_mockLogger.Object);
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
    public async Task BeginTransactionAsync_CompletesSuccessfully()
    {
        // Act
        var act = () => _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_LogsSkippedOperation()
    {
        // Arrange
        var (mock, entries) = LogCaptureBuilder.Create<UnitOfWork>();
        var unitOfWork = new UnitOfWork(mock.Object);

        // Act
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("BeginTransaction") &&
            e.Message.Contains("skipped"));
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_CompletesSuccessfully()
    {
        // Act
        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommitAsync_LogsSkippedOperation()
    {
        // Arrange
        var (mock, entries) = LogCaptureBuilder.Create<UnitOfWork>();
        var unitOfWork = new UnitOfWork(mock.Object);

        // Act
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("Commit") &&
            e.Message.Contains("skipped"));
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_CompletesSuccessfully()
    {
        // Act
        var act = () => _unitOfWork.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackAsync_LogsSkippedOperation()
    {
        // Arrange
        var (mock, entries) = LogCaptureBuilder.Create<UnitOfWork>();
        var unitOfWork = new UnitOfWork(mock.Object);

        // Act
        await unitOfWork.RollbackAsync();

        // Assert
        entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("Rollback") &&
            e.Message.Contains("skipped"));
    }

    // --- CheckConnectivityAsync ---

    [Fact]
    public async Task CheckConnectivityAsync_CompletesSuccessfully()
    {
        // Act
        var act = () => _unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckConnectivityAsync_LogsSkippedOperation()
    {
        // Arrange
        var (mock, entries) = LogCaptureBuilder.Create<UnitOfWork>();
        var unitOfWork = new UnitOfWork(mock.Object);

        // Act
        await unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Debug &&
            e.Message.Contains("CheckConnectivity") &&
            e.Message.Contains("skipped"));
    }
}
