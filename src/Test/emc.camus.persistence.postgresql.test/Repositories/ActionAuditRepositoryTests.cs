using System.Data;
using System.Data.Common;
using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class ActionAuditRepositoryTests : IDisposable
{
    private const string ActionTitle = "TestAction";
    private const string ActionSummary = "Test summary";
    private const string ActorUsername = "testuser";
    private const string SystemUsername = "System";
    private const string TraceId1 = "trace-123";
    private const string TraceId2 = "trace-456";
    private static readonly Guid UserId = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");

    private readonly Mock<IConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IActionAuditDataAccess> _mockDataAccess;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly UnitOfWork _unitOfWork;

    public ActionAuditRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>();
        _mockUserContext = new Mock<IUserContext>();
        _mockDataAccess = new Mock<IActionAuditDataAccess>();
        _mockConnection = new Mock<DbConnection>();

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
        // Act
        var act = () => new ActionAuditRepository(null!, _mockUserContext.Object, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ActionAuditRepository(_unitOfWork, null!, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userContext");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ActionAuditRepository(_unitOfWork, _mockUserContext.Object, null!);

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
        var act = () => repository.LogCurrentUserActionAsync(actionTitle!, ActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogCurrentUserActionAsync(ActionTitle, actionSummary!, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionSummary");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(default(Guid?));
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync(ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_NullUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(UserId);
        _mockUserContext.Setup(x => x.GetCurrentUsername()).Returns(default(string?));
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogCurrentUserActionAsync(ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Username*");
    }

    [Fact]
    public async Task LogCurrentUserActionAsync_ValidContext_ReturnsAuditId()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(UserId);
        _mockUserContext.Setup(x => x.GetCurrentUsername()).Returns(ActorUsername);
        _mockUserContext.Setup(x => x.GetCurrentTraceId()).Returns(TraceId1);
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDataAccess
            .Setup(d => d.InsertAsync(It.IsAny<IDbConnection>(), UserId, ActorUsername, TraceId1, ActionTitle, ActionSummary, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);
        var repository = CreateRepository();

        // Act
        var result = await repository.LogCurrentUserActionAsync(ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(42L);
    }

    // --- LogActionAsync ---

    [Fact]
    public async Task LogActionAsync_EmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(Guid.Empty, SystemUsername, ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>())
            .And.ParamName.Should().Be("userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogActionAsync_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(UserId, username!, ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogActionAsync(UserId, SystemUsername, actionTitle!, ActionSummary, TestContext.Current.CancellationToken);

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
        var act = () => repository.LogActionAsync(UserId, SystemUsername, ActionTitle, actionSummary!, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().Be("actionSummary");
    }

    [Fact]
    public async Task LogActionAsync_UserNotFound_ThrowsDataConflictException()
    {
        // Arrange
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var repository = CreateRepository();

        // Act
        var act = () => repository.LogActionAsync(UserId, ActorUsername, ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage($"*{UserId}*");
    }

    [Fact]
    public async Task LogActionAsync_UserExists_ReturnsAuditId()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentTraceId()).Returns(TraceId2);
        _mockDataAccess
            .Setup(d => d.UserExistsAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDataAccess
            .Setup(d => d.InsertAsync(It.IsAny<IDbConnection>(), UserId, ActorUsername, TraceId2, ActionTitle, ActionSummary, It.IsAny<CancellationToken>()))
            .ReturnsAsync(99L);
        var repository = CreateRepository();

        // Act
        var result = await repository.LogActionAsync(UserId, ActorUsername, ActionTitle, ActionSummary, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(99L);
    }
}
