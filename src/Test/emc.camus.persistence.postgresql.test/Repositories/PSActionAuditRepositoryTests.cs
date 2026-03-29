using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class PSActionAuditRepositoryTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IUserContext> _mockUserContext = new();

    private PSActionAuditRepository CreateRepository()
    {
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        return new PSActionAuditRepository(unitOfWork, _mockUserContext.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        PSUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new PSActionAuditRepository(unitOfWork!, _mockUserContext.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new PSActionAuditRepository(unitOfWork, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userContext");
    }

    // --- LogCurrentUserActionAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_NullOrWhitespaceTitle_ThrowsArgumentException(string? actionTitle)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync(actionTitle!, "Test summary");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionTitle");
    }

    // --- LogActionAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_NullOrWhitespaceUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, username!, "Test Action", "Test summary");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("username");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_NullOrWhitespaceTitle_ThrowsArgumentException(string? actionTitle)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "System", actionTitle!, "Test summary");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionTitle");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_NullOrWhitespaceSummary_ThrowsArgumentException(string? actionSummary)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "System", "Test Action", actionSummary!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionSummary");
    }
}
