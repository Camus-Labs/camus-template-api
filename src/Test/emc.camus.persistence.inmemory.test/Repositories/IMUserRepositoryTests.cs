using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.application.Secrets;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class IMUserRepositoryTests
{
    private readonly Mock<ISecretProvider> _mockSecretProvider = new();

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new IMUserRepository(null!, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => new IMUserRepository(settings, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_ValidSettings_DoesNotThrow()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);
        await repository.InitializeAsync();

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Theory]
    [MemberData(nameof(MissingSecretData))]
    public async Task InitializeAsync_MissingSecret_ThrowsInvalidOperationException(
        string usernameValue, string passwordValue, string expectedMessage)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, usernameValue);
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, passwordValue);
        var settings = CreateValidSettings();
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    public static TheoryData<string, string, string> MissingSecretData => new()
    {
        { "", "adminpass", $"*Failed to retrieve username*{InMemoryModelSettingsFactory.DefaultUsernameSecret}*" },
        { "adminuser", "", $"*Failed to retrieve password*{InMemoryModelSettingsFactory.DefaultPasswordSecret}*" }
    };

    // --- ValidateCredentialsAsync ---

    [Fact]
    public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync("adminuser", "adminpass");

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("adminuser");
        result.Roles.Should().ContainSingle().Which.Name.Should().Be(InMemoryModelSettingsFactory.DefaultRoleName);
    }

    [Theory]
    [InlineData("unknownuser", "adminpass", "*User not found*")]
    [InlineData("adminuser", "wrongpassword", "*mismatch*")]
    public async Task ValidateCredentialsAsync_InvalidCredentials_ThrowsUnauthorizedAccessException(
        string username, string password, string expectedMessage)
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync(username, password);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);

        // Act
        var act = () => repository.ValidateCredentialsAsync("adminuser", "adminpass");

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
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync(username!, "adminpass");

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
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.ValidateCredentialsAsync("adminuser", password!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var user = await repository.ValidateCredentialsAsync("adminuser", "adminpass");

        // Act
        var result = await repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("adminuser");
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var nonExistentId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Act
        var act = () => repository.GetByIdAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.GetByIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetByIdAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);
        var userId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var act = () => repository.GetByIdAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    // --- UpdateLastLoginAsync ---

    [Fact]
    public async Task UpdateLastLoginAsync_ValidUserId_CompletesSuccessfully()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);
        var user = await repository.ValidateCredentialsAsync("adminuser", "adminpass");

        // Act
        var act = () => repository.UpdateLastLoginAsync(user.Id);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateLastLoginAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultUsernameSecret, "adminuser");
        SetupSecretProvider(InMemoryModelSettingsFactory.DefaultPasswordSecret, "adminpass");
        var settings = CreateValidSettings();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var act = () => repository.UpdateLastLoginAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // --- Initialize with multiple users and roles ---

    [Fact]
    public async Task ValidateCredentialsAsync_MultipleUsers_ReturnsCorrectUser()
    {
        // Arrange
        SetupSecretProvider("admin-username", "adminuser");
        SetupSecretProvider("admin-password", "adminpass");
        SetupSecretProvider("reader-username", "readeruser");
        SetupSecretProvider("reader-password", "readerpass");
        var settings = CreateSettingsWithMultipleUsers();
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync("readeruser", "readerpass");

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("readeruser");
        result.Roles.Should().ContainSingle().Which.Name.Should().Be("reader");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UserWithMultipleRoles_ReturnsAllRoles()
    {
        // Arrange
        SetupSecretProvider("admin-username", "adminuser");
        SetupSecretProvider("admin-password", "adminpass");
        var settings = new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings { Name = "admin", Permissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite } },
                new RoleSettings { Name = "token-manager", Permissions = new List<string> { Permissions.TokenCreate } }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "admin-username",
                    PasswordSecretName = "admin-password",
                    Roles = new List<string> { "admin", "token-manager" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>()
        };
        var repository = await CreateInitializedRepository(settings);

        // Act
        var result = await repository.ValidateCredentialsAsync("adminuser", "adminpass");

        // Assert
        result.Roles.Should().HaveCount(2);
        result.Roles.Select(r => r.Name).Should().BeEquivalentTo("admin", "token-manager");
    }

    private void SetupSecretProvider(string secretName, string secretValue)
    {
        _mockSecretProvider
            .Setup(sp => sp.GetSecret(secretName))
            .Returns(secretValue);
    }

    private async Task<IMUserRepository> CreateInitializedRepository(InMemoryModelSettings settings)
    {
        var repository = new IMUserRepository(settings, _mockSecretProvider.Object);
        await repository.InitializeAsync();
        return repository;
    }

    private static InMemoryModelSettings CreateValidSettings()
    {
        return InMemoryModelSettingsFactory.Create(apiInfos: new List<ApiInfoSettings>());
    }

    private static InMemoryModelSettings CreateSettingsWithMultipleUsers()
    {
        return new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings
                {
                    Name = "admin",
                    Permissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite }
                },
                new RoleSettings
                {
                    Name = "reader",
                    Permissions = new List<string> { Permissions.ApiRead }
                }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "admin-username",
                    PasswordSecretName = "admin-password",
                    Roles = new List<string> { "admin" }
                },
                new UserSettings
                {
                    UsernameSecretName = "reader-username",
                    PasswordSecretName = "reader-password",
                    Roles = new List<string> { "reader" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>()
        };
    }
}
