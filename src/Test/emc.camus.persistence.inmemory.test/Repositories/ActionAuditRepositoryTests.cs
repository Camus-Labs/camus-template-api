using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.persistence.inmemory.Repositories;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class ActionAuditRepositoryTests
{
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
        // Arrange
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
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        userContextMock.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

        // Act
        var result = await repository.LogCurrentUserActionAsync("TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

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
        userContextMock.Setup(x => x.GetCurrentUserId()).Returns(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        userContextMock.Setup(x => x.GetCurrentUsername()).Returns((string?)null);
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

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
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync(title!, "Test summary", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- LogActionAsync ---

    [Fact]
    public async Task LogActionAsync_ValidParameters_ReturnsZero()
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);
        var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var result = await repository.LogActionAsync(userId, "admin", "TestAction", "Test summary", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0L);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var userContextMock = new Mock<IUserContext>();
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, username!, "TestAction", "Test summary", TestContext.Current.CancellationToken);

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
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "admin", title!, "Test summary", TestContext.Current.CancellationToken);

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
        var repository = new ActionAuditRepository(NullLogger<ActionAuditRepository>.Instance, userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "admin", "TestAction", summary!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
