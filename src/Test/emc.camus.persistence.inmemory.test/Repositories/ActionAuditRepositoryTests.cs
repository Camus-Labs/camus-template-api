using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.persistence.inmemory.Repositories;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class ActionAuditRepositoryTests
{
    private const string ValidActionTitle = "TestAction";
    private const string ValidActionSummary = "Test summary";
    private const string ValidUsername = "admin";
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ILogger<ActionAuditRepository>> _loggerMock;

    public ActionAuditRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<ActionAuditRepository>>();
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
    public async Task LogCurrentUserActionAsync_ValidTitle_ReturnsZero()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(ValidUserId);
        userContextMock.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var result = await repository.LogCurrentUserActionAsync(ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(default(Guid?));
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync(ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogCurrentUserActionAsync(ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogCurrentUserActionAsync(title!, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogCurrentUserActionAsync(ValidActionTitle, summary!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- LogActionAsync ---

    [Fact]
    public async Task LogActionAsync_ValidParameters_ReturnsZero()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var result = await repository.LogActionAsync(ValidUserId, ValidUsername, ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
    }

    [Fact]
    public async Task LogActionAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(_loggerMock.Object, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, ValidUsername, ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogActionAsync(ValidUserId, username!, ValidActionTitle, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogActionAsync(ValidUserId, ValidUsername, title!, ValidActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogActionAsync(ValidUserId, ValidUsername, ValidActionTitle, summary!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
