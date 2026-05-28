using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.application.Exceptions;
using emc.camus.application.Secrets;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class UserRepositoryTests
{
    private const string TestUsername = "adminuser";
    private const string TestPassword = "adminpass";
    private const string AdminUsernameSecret = "admin-username";
    private const string AdminPasswordSecret = "admin-password";
    private const string ReaderUsernameSecret = "reader-username";
    private const string ReaderPasswordSecret = "reader-password";
    private const string ReaderUsername = "readeruser";
    private const string ReaderPassword = "readerpass";
    private const string AdminRoleName = "admin";
    private const string ReaderRoleName = "reader";
    private const string TokenManagerRoleName = "token-manager";
    private static readonly List<string> AdminPermissions = [Permissions.ApiRead, Permissions.ApiWrite];
    private static readonly List<string> TokenManagerPermissions = [Permissions.TokenCreate];
    private static readonly List<string> ReaderPermissions = [Permissions.ApiRead];
    private static readonly List<string> AdminAndTokenManagerRoleNames = [AdminRoleName, TokenManagerRoleName];
    private static readonly List<string> AdminRoleNames = [AdminRoleName];
    private static readonly List<string> ReaderRoleNames = [ReaderRoleName];
    private static readonly List<ApiInfoSettings> EmptyApiInfos = [];
    private static readonly List<RoleSettings> AdminAndTokenManagerRoles =
    [
        new RoleSettings { Name = AdminRoleName, Permissions = AdminPermissions },
        new RoleSettings { Name = TokenManagerRoleName, Permissions = TokenManagerPermissions }
    ];
    private static readonly List<UserSettings> SingleAdminWithBothRolesUsers =
    [
        new UserSettings
        {
            UsernameSecretName = AdminUsernameSecret,
            PasswordSecretName = AdminPasswordSecret,
            Roles = AdminAndTokenManagerRoleNames
        }
    ];
    private static readonly List<RoleSettings> AdminAndReaderRoles =
    [
        new RoleSettings
        {
            Name = AdminRoleName,
            Permissions = AdminPermissions
        },
        new RoleSettings
        {
            Name = ReaderRoleName,
            Permissions = ReaderPermissions
        }
    ];
    private static readonly List<UserSettings> AdminAndReaderUsers =
    [
        new UserSettings
        {
            UsernameSecretName = AdminUsernameSecret,
            PasswordSecretName = AdminPasswordSecret,
            Roles = AdminRoleNames
        },
        new UserSettings
        {
            UsernameSecretName = ReaderUsernameSecret,
            PasswordSecretName = ReaderPasswordSecret,
            Roles = ReaderRoleNames
        }
    ];
    private readonly Mock<ISecretProvider> _mockSecretProvider;

    public UserRepositoryTests()
    {
        _mockSecretProvider = new Mock<ISecretProvider>();
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new UserRepository(null!, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => new UserRepository(settings, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_ValidSettings_DoesNotThrow()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = new UserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = new UserRepository(settings, _mockSecretProvider.Object);
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Theory]
    [InlineData("", TestPassword, "*Failed to retrieve username*admin-username*")]
    [InlineData(TestUsername, "", "*Failed to retrieve password*admin-password*")]
    public async Task InitializeAsync_MissingSecret_ThrowsInvalidOperationException(
        string usernameValue, string passwordValue, string expectedMessage)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, usernameValue);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, passwordValue);
        var settings = CreateValidSettings();
        var repository = new UserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public async Task InitializeAsync_DuplicateResolvedUsername_ThrowsDataConflictException()
    {
        // Arrange
        var duplicateUsers = new List<UserSettings>
        {
            new() { UsernameSecretName = AdminUsernameSecret, PasswordSecretName = AdminPasswordSecret, Roles = AdminRoleNames },
            new() { UsernameSecretName = ReaderUsernameSecret, PasswordSecretName = ReaderPasswordSecret, Roles = AdminRoleNames }
        };
        var settings = new InMemoryModelSettings
        {
            Roles = AdminAndReaderRoles,
            Users = duplicateUsers,
            ApiInfos = EmptyApiInfos
        };
        SetupSecretProvider(AdminUsernameSecret, TestUsername);
        SetupSecretProvider(AdminPasswordSecret, TestPassword);
        SetupSecretProvider(ReaderUsernameSecret, TestUsername);
        SetupSecretProvider(ReaderPasswordSecret, ReaderPassword);
        var repository = new UserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*Duplicate*username*");
    }

    // --- ValidateCredentialsAsync ---

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync(TestUsername, TestPassword, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(TestUsername);
        result.Roles.Should().ContainSingle().Which.Name.Should().Be(InMemoryModelSettingsFactory.DefaultRoleName);
    }

    [Theory]
    [InlineData("ADMINUSER")]
    [InlineData("AdminUser")]
    [InlineData("aDmInUsEr")]
    public async Task ValidateCredentialsAsync_CaseInsensitiveUsername_ReturnsUser(string username)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync(username, TestPassword, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(TestUsername);
    }

    [Theory]
    [InlineData("unknownuser", TestPassword, "*User not found*")]
    [InlineData(TestUsername, "wrongpassword", "*mismatch*")]
    public async Task ValidateCredentialsAsync_InvalidCredentials_ThrowsUnauthorizedAccessException(
        string username, string password, string expectedMessage)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync(username, password, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new UserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.ValidateCredentialsAsync(TestUsername, TestPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateCredentialsAsync_InvalidUsernameParam_ThrowsArgumentException(string? username)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync(username!, TestPassword, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateCredentialsAsync_InvalidPasswordParam_ThrowsArgumentException(string? password)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync(TestUsername, password!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var user = await repository.ValidateCredentialsAsync(TestUsername, TestPassword, TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(TestUsername);
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var nonExistentId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Act
        var act = () => repository.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.GetByIdAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetByIdAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new UserRepository(settings, _mockSecretProvider.Object);
        var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var act = () => repository.GetByIdAsync(userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    // --- UpdateLastLoginAsync ---

    [Fact]
    public async Task UpdateLastLoginAsync_ValidUserId_CompletesSuccessfully()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var user = await repository.ValidateCredentialsAsync(TestUsername, TestPassword, TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.UpdateLastLoginAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateLastLoginAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, TestUsername);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, TestPassword);
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.UpdateLastLoginAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // --- Initialize with multiple users and roles ---

    [Fact]
    public async Task ValidateCredentialsAsync_MultipleUsers_ReturnsCorrectUser()
    {
        // Arrange
        SetupSecretProvider(AdminUsernameSecret, TestUsername);
        SetupSecretProvider(AdminPasswordSecret, TestPassword);
        SetupSecretProvider(ReaderUsernameSecret, ReaderUsername);
        SetupSecretProvider(ReaderPasswordSecret, ReaderPassword);
        var settings = CreateSettingsWithMultipleUsers();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync(ReaderUsername, ReaderPassword, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(ReaderUsername);
        result.Roles.Should().ContainSingle().Which.Name.Should().Be(ReaderRoleName);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UserWithMultipleRoles_ReturnsAllRoles()
    {
        // Arrange
        SetupSecretProvider(AdminUsernameSecret, TestUsername);
        SetupSecretProvider(AdminPasswordSecret, TestPassword);
        var settings = new InMemoryModelSettings
        {
            Roles = AdminAndTokenManagerRoles,
            Users = SingleAdminWithBothRolesUsers,
            ApiInfos = EmptyApiInfos
        };
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync(TestUsername, TestPassword, TestContext.Current.CancellationToken);

        // Assert
        result.Roles.Should().HaveCount(2);
        result.Roles.Select(r => r.Name).Should().BeEquivalentTo(AdminRoleName, TokenManagerRoleName);
    }

    private void SetupSecretProvider(string secretName, string secretValue)
    {
        _mockSecretProvider
            .Setup(sp => sp.GetSecret(secretName))
            .Returns(secretValue);
    }

    private async Task<UserRepository> CreateInitializedRepository(InMemoryModelSettings settings)
    {
        var repository = new UserRepository(settings, _mockSecretProvider.Object);
        await repository.InitializeAsync(TestContext.Current.CancellationToken);
        return repository;
    }

    private static InMemoryModelSettings CreateValidSettings()
    {
        return InMemoryModelSettingsFactory.Create(apiInfos: EmptyApiInfos);
    }

    private static InMemoryModelSettings CreateSettingsWithMultipleUsers()
    {
        return new InMemoryModelSettings
        {
            Roles = AdminAndReaderRoles,
            Users = AdminAndReaderUsers,
            ApiInfos = EmptyApiInfos
        };
    }
}
