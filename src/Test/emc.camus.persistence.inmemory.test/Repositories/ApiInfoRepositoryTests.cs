using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class ApiInfoRepositoryTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new ApiInfoRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_ValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new ApiInfoRepository(settings);

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task InitializeAsync_EmptyApiInfos_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.ApiInfos = new List<ApiInfoSettings>();
        var repository = new ApiInfoRepository(settings);

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_NullFeatures_DefaultsToEmptyList()
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: new List<ApiInfoSettings>
        {
            new ApiInfoSettings
            {
                Name = InMemoryModelSettingsFactory.DefaultApiName,
                Version = InMemoryModelSettingsFactory.DefaultApiVersion,
                Status = InMemoryModelSettingsFactory.DefaultApiStatus,
                Features = null!
            }
        });
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

        // Act
        var result = await repository.GetByVersionAsync(InMemoryModelSettingsFactory.DefaultApiVersion);

        // Assert
        result.Features.Should().BeEmpty();
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_ExistingVersion_ReturnsApiInfo()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

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
        var settings = CreateValidSettings(apiInfos: new List<ApiInfoSettings>
        {
            new ApiInfoSettings
            {
                Name = "Test API",
                Version = "V1",
                Status = "Available",
                Features = new List<string>()
            }
        });
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

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
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

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
        var repository = new ApiInfoRepository(settings);

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
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

        // Act
        var act = () => repository.GetByVersionAsync(version!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InitializeAsync_ApiInfoWithoutName_ThrowsArgumentException()
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: new List<ApiInfoSettings>
        {
            new ApiInfoSettings
            {
                Name = "",
                Version = "1.0",
                Status = "Available",
                Features = new List<string>()
            }
        });
        var repository = new ApiInfoRepository(settings);

        // Act
        var act = () => repository.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByVersionAsync_ApiInfoWithEmptyFeatures_ReturnsEmptyFeatures()
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: new List<ApiInfoSettings>
        {
            new ApiInfoSettings
            {
                Name = "Test API",
                Version = "1.0",
                Status = "Available",
                Features = new List<string>()
            }
        });
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync();

        // Act
        var result = await repository.GetByVersionAsync("1.0");

        // Assert
        result.Features.Should().BeEmpty();
    }

    private static InMemoryModelSettings CreateValidSettings(List<ApiInfoSettings>? apiInfos = null)
    {
        return InMemoryModelSettingsFactory.Create(apiInfos: apiInfos);
    }
}
