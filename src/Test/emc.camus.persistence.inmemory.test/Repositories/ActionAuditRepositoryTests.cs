using System.Collections.Concurrent;
using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class ActionAuditRepositoryTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ILogger<ActionAuditRepository>> _loggerMock;
    private readonly ConcurrentBag<(LogLevel Level, string Message)> _logEntries;

    public ActionAuditRepositoryTests()
    {
        (_loggerMock, _logEntries) = LogCaptureBuilder.Create<ActionAuditRepository>();
    }
    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();

        // Act
        var act = () => new ActionAuditRepository(null!, userContextMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- LogCurrentUserActionAsync ---

    [Fact]
    public async Task LogCurrentUserActionAsync_ValidTitle_ReturnsZeroAndLogsAudit()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(ValidUserId);
        userContextMock.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var result = await repository.LogCurrentUserActionAsync("TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("testuser") && e.Message.Contains("TestAction"));
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(default(Guid?));
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(ValidUserId);
        userContextMock.Setup(x => x.GetCurrentUsername()).Returns(default(string?));
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Username*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_InvalidTitle_ThrowsArgumentException(string? title)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync(title!, "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_InvalidActionSummary_ThrowsArgumentException(string? summary)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", summary!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- LogActionAsync ---

    [Fact]
    public async Task LogActionAsync_ValidParameters_ReturnsZeroAndLogsAudit()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var result = await repository.LogActionAsync(ValidUserId, "admin", "TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("admin") && e.Message.Contains("TestAction"));
    }

    [Fact]
    public async Task LogActionAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "admin", "TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(ValidUserId, username!, "TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidActionTitle_ThrowsArgumentException(string? title)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(ValidUserId, "admin", title!, "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidActionSummary_ThrowsArgumentException(string? summary)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(ValidUserId, "admin", "TestAction", summary!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
