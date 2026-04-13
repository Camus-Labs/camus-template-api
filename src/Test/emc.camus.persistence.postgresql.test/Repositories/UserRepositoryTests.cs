using System.Data;
using System.Data.Common;
using FluentAssertions;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class UserRepositoryTests : IDisposable
{
    private static readonly Guid UserId = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");
    private static readonly string Username = "testuser";
    private static readonly string PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
    private static readonly string[] RolePermissions = new[] { "read", "write" };

    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IUserDataAccess> _mockDataAccess = new();
    private readonly Mock<DbConnection> _mockConnection = new();
    private readonly UnitOfWork _unitOfWork;

    public UserRepositoryTests()
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

    private UserRepository CreateRepository()
    {
        return new UserRepository(_unitOfWork, new InitializationState(), _mockDataAccess.Object);
    }

    private UserRepository CreateInitializedRepository()
    {
        var initState = new InitializationState { UserRepositoryInitialized = true };
        return new UserRepository(_unitOfWork, initState, _mockDataAccess.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        UnitOfWork? unitOfWork = null;

        // Act
        var act = () => new UserRepository(unitOfWork!, new InitializationState(), _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullInitState_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new UserRepository(unitOfWork, null!, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("initState");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new UserRepository(unitOfWork, new InitializationState(), null!);

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
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task InitializeAsync_AllTablesExist_SetsInitializedState()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CheckRequiredTablesAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>
            {
                ["users"] = true,
                ["roles"] = true,
                ["user_roles"] = true,
                ["role_permissions"] = true,
            });

        // Act
        await repository.InitializeAsync();

        // Assert — calling again should throw "already initialized"
        var act = () => repository.InitializeAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task InitializeAsync_MissingTables_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CheckRequiredTablesAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>
            {
                ["users"] = true,
                ["roles"] = false,
                ["user_roles"] = true,
                ["role_permissions"] = false,
            });

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*roles*role_permissions*");
    }

    // --- ValidateCredentialsAsync ---

    [Fact]
    public async Task ValidateCredentialsAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.ValidateCredentialsAsync("user", "pass");

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
        var act = () => repository.ValidateCredentialsAsync(username!, "pass");

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
        var act = () => repository.ValidateCredentialsAsync("user", password!);

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
            .ReturnsAsync((UserModel?)null);

        // Act
        var act = () => repository.ValidateCredentialsAsync(Username, "anypassword");

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
        var act = () => repository.ValidateCredentialsAsync(Username, "wrongpassword");

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
        var act = () => repository.ValidateCredentialsAsync(Username, "anypassword");

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
            .ReturnsAsync(new[]
            {
                new RoleModel { Id = Guid.Parse("b2c3d4e5-0001-0002-0003-000000000002"), Name = "admin", Description = "Administrator", Permissions = RolePermissions }
            });

        // Act
        var result = await repository.ValidateCredentialsAsync(Username, "correctpassword");

        // Assert
        result.Id.Should().Be(UserId);
        result.Username.Should().Be(Username);
        result.Roles.Should().ContainSingle()
            .Which.Name.Should().Be("admin");
        result.Roles[0].Permissions.Should().BeEquivalentTo(RolePermissions);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByIdAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserModel?)null);

        // Act
        var act = () => repository.GetByIdAsync(UserId);

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
            .ReturnsAsync(Array.Empty<RoleModel>());

        // Act
        var result = await repository.GetByIdAsync(UserId);

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
        var act = () => repository.UpdateLastLoginAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task UpdateLastLoginAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.UpdateLastLoginAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var act = () => repository.UpdateLastLoginAsync(UserId);

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
            .Setup(d => d.UpdateLastLoginAsync(It.IsAny<IDbConnection>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var act = () => repository.UpdateLastLoginAsync(UserId);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
