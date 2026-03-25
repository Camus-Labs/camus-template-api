using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using emc.camus.persistence.inmemory.Repositories;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class IMActionAuditRepositoryTests
{
    private readonly ILogger<IMActionAuditRepository> _logger = NullLogger<IMActionAuditRepository>.Instance;
    private readonly Mock<IUserContext> _userContextMock = new();

    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new IMActionAuditRepository(null!, _userContextMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new IMActionAuditRepository(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- LogCurrentUserActionAsync ---

    [Fact]
    public async Task LogCurrentUserActionAsync_ValidTitle_ReturnsZero()
    {
        // Arrange
        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.Empty);
        _userContextMock.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);

        // Act
        var result = await repository.LogCurrentUserActionAsync("TestAction", "Test summary");

        // Assert
        result.Should().Be(0L);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_InvalidTitle_ThrowsArgumentException(string? title)
    {
        // Arrange
        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(Guid.Empty);
        _userContextMock.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);

        // Act
        var act = () => repository.LogCurrentUserActionAsync(title!, "Test summary");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- LogActionAsync ---

    [Fact]
    public async Task LogActionAsync_ValidParameters_ReturnsZero()
    {
        // Arrange
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);
        var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var result = await repository.LogActionAsync(userId, "admin", "TestAction", "Test summary");

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
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, username!, "TestAction", "Test summary");

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
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "admin", title!, "Test summary");

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
        var repository = new IMActionAuditRepository(_logger, _userContextMock.Object);

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "admin", "TestAction", summary!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
