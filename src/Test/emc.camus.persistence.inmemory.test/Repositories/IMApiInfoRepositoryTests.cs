using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class IMApiInfoRepositoryTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new IMApiInfoRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Initialize ---

    [Fact]
    public void Initialize_ValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);

        // Act
        var act = () => repository.Initialize();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var act = () => repository.Initialize();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public void Initialize_EmptyApiInfos_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.ApiInfos = new List<ApiInfoSettings>();
        var repository = new IMApiInfoRepository(settings);

        // Act
        var act = () => repository.Initialize();

        // Assert
        act.Should().NotThrow();
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_ExistingVersion_ReturnsApiInfo()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var result = await repository.GetByVersionAsync(InMemoryModelSettingsFactory.DefaultApiVersion);

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be(InMemoryModelSettingsFactory.DefaultApiVersion);
        result.Name.Should().Be(InMemoryModelSettingsFactory.DefaultApiName);
        result.Status.Should().Be(InMemoryModelSettingsFactory.DefaultApiStatus);
        result.Features.Should().ContainSingle().Which.Should().Be(InMemoryModelSettingsFactory.DefaultApiFeature);
    }

    [Fact]
    public async Task GetByVersionAsync_CaseInsensitiveLookup_ReturnsApiInfo()
    {
        // Arrange
        var settings = new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings { Name = "admin", Permissions = new List<string> { Permissions.ApiRead } }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "user-secret",
                    PasswordSecretName = "pass-secret",
                    Roles = new List<string> { "admin" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>
            {
                new ApiInfoSettings
                {
                    Name = "Test API",
                    Version = "V1",
                    Status = "Available",
                    Features = new List<string>()
                }
            }
        };
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var result = await repository.GetByVersionAsync("v1");

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be("V1");
    }

    [Fact]
    public async Task GetByVersionAsync_NonExistingVersion_ThrowsKeyNotFoundException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var act = () => repository.GetByVersionAsync("99.0");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*99.0*");
    }

    [Fact]
    public async Task GetByVersionAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);

        // Act
        var act = () => repository.GetByVersionAsync("1.0");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByVersionAsync_InvalidVersion_ThrowsArgumentException(string? version)
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var act = () => repository.GetByVersionAsync(version!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByVersionAsync_ApiInfoWithoutName_UsesDefaultName()
    {
        // Arrange
        var settings = new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings { Name = "admin", Permissions = new List<string> { Permissions.ApiRead } }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "user-secret",
                    PasswordSecretName = "pass-secret",
                    Roles = new List<string> { "admin" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>
            {
                new ApiInfoSettings
                {
                    Name = "",
                    Version = "1.0",
                    Status = "Available",
                    Features = new List<string>()
                }
            }
        };
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var result = await repository.GetByVersionAsync("1.0");

        // Assert
        result.Name.Should().Be("My Basic API");
    }

    [Fact]
    public async Task GetByVersionAsync_ApiInfoWithEmptyFeatures_ReturnsEmptyFeatures()
    {
        // Arrange
        var settings = new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings { Name = "admin", Permissions = new List<string> { Permissions.ApiRead } }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "user-secret",
                    PasswordSecretName = "pass-secret",
                    Roles = new List<string> { "admin" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>
            {
                new ApiInfoSettings
                {
                    Name = "Test API",
                    Version = "1.0",
                    Status = "Available",
                    Features = new List<string>()
                }
            }
        };
        var repository = new IMApiInfoRepository(settings);
        repository.Initialize();

        // Act
        var result = await repository.GetByVersionAsync("1.0");

        // Assert
        result.Features.Should().BeEmpty();
    }

    private static InMemoryModelSettings CreateValidSettings()
    {
        return InMemoryModelSettingsFactory.Create();
    }
}
