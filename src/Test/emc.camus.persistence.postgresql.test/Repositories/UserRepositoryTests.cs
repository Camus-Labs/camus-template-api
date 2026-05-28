using System.Data;
using System.Data.Common;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class UserRepositoryTests : IDisposable
{
    private static readonly Guid UserId = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");
    private const string Username = "testuser";
    private const string PasswordHash = "$2a$11$abcdefghijklmnopqrstuOVxsQf1QFlQ8j3oEjFaXIgGff.td6/we";
    private const string DummyUsername = "user";
    private const string DummyPassword = "pass";
    private const string AnyPassword = "anypassword";
    private const string AdminRoleName = "admin";
    private static readonly string[] RolePermissions = new[] { "read", "write" };
    private static readonly Guid AdminRoleId = Guid.Parse("b2c3d4e5-0001-0002-0003-000000000002");
    private static readonly RoleModel[] AdminRoles = new[]
    {
        new RoleModel { Id = AdminRoleId, Name = AdminRoleName, Description = "Administrator", Permissions = RolePermissions }
    };
    private static readonly RoleModel[] EmptyRoles = [];
    private static readonly DateTimeOffset ReferenceTime = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IUserDataAccess> _mockDataAccess;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly FakeTimeProvider _timeProvider;
    private readonly UnitOfWork _unitOfWork;

    public UserRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>();
        _mockDataAccess = new Mock<IUserDataAccess>();
        _mockConnection = new Mock<DbConnection>();
        _timeProvider = new FakeTimeProvider(ReferenceTime);

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

    private UserRepository CreateRepository()
    {
        return new UserRepository(_unitOfWork, new InitializationState(), _mockDataAccess.Object, _timeProvider);
    }

    private UserRepository CreateInitializedRepository()
    {
        var initState = new InitializationState { UserRepositoryInitialized = true };
        return new UserRepository(_unitOfWork, initState, _mockDataAccess.Object, _timeProvider);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UserRepository(null!, new InitializationState(), _mockDataAccess.Object, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullInitState_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UserRepository(_unitOfWork, null!, _mockDataAccess.Object, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("initState");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new UserRepository(_unitOfWork, new InitializationState(), null!, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("dataAccess");
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_AlreadyInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateInitializedRepository();

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task InitializeAsync_AllTablesExist_SetsInitializedState()
    {
        // Arrange
        var initState = new InitializationState();
        var repository = new UserRepository(_unitOfWork, initState, _mockDataAccess.Object, _timeProvider);
        _mockDataAccess
            .Setup(d => d.CheckRequiredTablesAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TableExistenceModel
            {
                UsersExists = true,
                RolesExists = true,
                UserRolesExists = true,
                RolePermissionsExists = true,
            });

        // Act
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        initState.UserRepositoryInitialized.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, false, "*roles*role_permissions*")]
    [InlineData(false, true, "*roles*")]
    public async Task InitializeAsync_MissingTables_ThrowsInvalidOperationException(
        bool rolesExists, bool rolePermissionsExists, string expectedMessagePattern)
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CheckRequiredTablesAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TableExistenceModel
            {
                UsersExists = true,
                RolesExists = rolesExists,
                UserRolesExists = true,
                RolePermissionsExists = rolePermissionsExists,
            });

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expectedMessagePattern);
    }

    // --- ValidateCredentialsAsync ---

    [Fact]
    public async Task ValidateCredentialsAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.ValidateCredentialsAsync(DummyUsername, DummyPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateCredentialsAsync_InvalidUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        var repository = CreateInitializedRepository();

        // Act
        var act = () => repository.ValidateCredentialsAsync(username!, DummyPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "username");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateCredentialsAsync_InvalidPassword_ThrowsArgumentException(string? password)
    {
        // Arrange
        var repository = CreateInitializedRepository();

        // Act
        var act = () => repository.ValidateCredentialsAsync(DummyUsername, password!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "password");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByUsernameWithHashAsync(It.IsAny<IDbConnection>(), Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(UserModel?));

        // Act
        var act = () => repository.ValidateCredentialsAsync(Username, AnyPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByUsernameWithHashAsync(It.IsAny<IDbConnection>(), Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { Id = UserId, Username = Username, PasswordHash = PasswordHash });

        // Act
        var act = () => repository.ValidateCredentialsAsync(Username, "wrongpassword", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*mismatch*");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_CorruptedHash_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByUsernameWithHashAsync(It.IsAny<IDbConnection>(), Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { Id = UserId, Username = Username, PasswordHash = "not-a-valid-hash" });

        // Act
        var act = () => repository.ValidateCredentialsAsync(Username, AnyPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*corrupted*");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsUserWithRoles()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByUsernameWithHashAsync(It.IsAny<IDbConnection>(), Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { Id = UserId, Username = Username, PasswordHash = PasswordHash });
        _mockDataAccess
            .Setup(d => d.GetRolesByUserIdAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminRoles);

        // Act
        var result = await repository.ValidateCredentialsAsync(Username, "correctpassword", TestContext.Current.CancellationToken);

        // Assert
        result.Id.Should().Be(UserId);
        result.Username.Should().Be(Username);
        result.Roles.Should().ContainSingle()
            .Which.Name.Should().Be(AdminRoleName);
        result.Roles[0].Permissions.Should().BeEquivalentTo(RolePermissions);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByIdAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task GetByIdAsync_EmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByIdAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>())
            .And.ParamName.Should().Be("userId");
    }

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByIdAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(UserModel?));

        // Act
        var act = () => repository.GetByIdAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{UserId}*");
    }

    [Fact]
    public async Task GetByIdAsync_UserFound_ReturnsUserWithRoles()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByIdAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { Id = UserId, Username = Username });
        _mockDataAccess
            .Setup(d => d.GetRolesByUserIdAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyRoles);

        // Act
        var result = await repository.GetByIdAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        result.Id.Should().Be(UserId);
        result.Username.Should().Be(Username);
        result.Roles.Should().BeEmpty();
    }

    // --- UpdateLastLoginAsync ---

    [Fact]
    public async Task UpdateLastLoginAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.UpdateLastLoginAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task UpdateLastLoginAsync_EmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.UpdateLastLoginAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>())
            .And.ParamName.Should().Be("userId");
    }

    [Fact]
    public async Task UpdateLastLoginAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.UpdateLastLoginAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var act = () => repository.UpdateLastLoginAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{UserId}*");
    }

    [Fact]
    public async Task UpdateLastLoginAsync_UserExists_CompletesSuccessfully()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.UpdateLastLoginAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var act = () => repository.UpdateLastLoginAsync(UserId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
