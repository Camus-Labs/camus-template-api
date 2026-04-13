using System.Data;
using System.Data.Common;
using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class ActionAuditRepositoryTests : IDisposable
{
    private static readonly Guid UserId = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");

    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly Mock<IActionAuditDataAccess> _mockDataAccess = new();
    private readonly Mock<DbConnection> _mockConnection = new();
    private readonly UnitOfWork _unitOfWork;

    public ActionAuditRepositoryTests()
    {
        _mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockConnection.Object);

        _unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        GC.SuppressFinalize(this);
    }

    private ActionAuditRepository CreateRepository()
    {
        return new ActionAuditRepository(_unitOfWork, _mockUserContext.Object, _mockDataAccess.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        UnitOfWork? unitOfWork = null;

        // Act
        var act = () => new ActionAuditRepository(unitOfWork!, _mockUserContext.Object, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new ActionAuditRepository(unitOfWork, null!, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userContext");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new ActionAuditRepository(unitOfWork, _mockUserContext.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("dataAccess");
    }

    // --- LogCurrentUserActionAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_InvalidTitle_ThrowsArgumentException(string? actionTitle)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync(actionTitle!, "Test summary");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionTitle");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogCurrentUserActionAsync_InvalidSummary_ThrowsArgumentException(string? actionSummary)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", actionSummary!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionSummary");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", "Test summary");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(UserId);
        _mockUserContext.Setup(x => x.GetCurrentUsername()).Returns((string?)null);
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync("TestAction", "Test summary");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Username*");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_ValidContext_ReturnsAuditId()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(UserId);
        _mockUserContext.Setup(x => x.GetCurrentUsername()).Returns("testuser");
        _mockUserContext.Setup(x => x.GetCurrentTraceId()).Returns("trace-123");
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDataAccess
            .Setup(d => d.InsertAsync(It.IsAny<IDbConnection>(), UserId, "testuser", "trace-123", "TestAction", "Test summary", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);
        var repository = CreateRepository();

        // Act
        var result = await repository.LogCurrentUserActionAsync("TestAction", "Test summary");

        // Assert
        result.Should().Be(42L);
    }

    // --- LogActionAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidUsername_ThrowsArgumentException(string? username)
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
    public async Task LogActionAsync_InvalidTitle_ThrowsArgumentException(string? actionTitle)
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
    public async Task LogActionAsync_InvalidSummary_ThrowsArgumentException(string? actionSummary)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, "System", "Test Action", actionSummary!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionSummary");
    }

    [Fact]
    public async Task LogActionAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(UserId, "testuser", "TestAction", "Test summary");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{UserId}*");
    }

    [Fact]
    public async Task LogActionAsync_UserExists_ReturnsAuditId()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentTraceId()).Returns("trace-456");
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDataAccess
            .Setup(d => d.InsertAsync(It.IsAny<IDbConnection>(), UserId, "testuser", "trace-456", "TestAction", "Test summary", It.IsAny<CancellationToken>()))
            .ReturnsAsync(99L);
        var repository = CreateRepository();

        // Act
        var result = await repository.LogActionAsync(UserId, "testuser", "TestAction", "Test summary");

        // Assert
        result.Should().Be(99L);
    }
}
