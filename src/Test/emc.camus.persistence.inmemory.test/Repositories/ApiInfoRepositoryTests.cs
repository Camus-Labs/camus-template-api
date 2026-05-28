using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.Repositories;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Repositories;

public class ApiInfoRepositoryTests
{
    private const string ValidVersion = "1.0";
    private const string ValidStatus = "Available";
    private static readonly List<string> EmptyFeatureArray = [];
    private static readonly List<ApiInfoSettings> EmptyApiInfoArray = [];
    private static readonly List<ApiInfoSettings> CaseInsensitiveApiInfoArray =
    [
        new ApiInfoSettings
        {
            Name = "Test API",
            Version = "V1",
            Status = ValidStatus,
            Features = EmptyFeatureArray
        }
    ];
    private static readonly List<ApiInfoSettings> EmptyNameApiInfoArray =
    [
        new ApiInfoSettings
        {
            Name = "",
            Version = ValidVersion,
            Status = ValidStatus,
            Features = EmptyFeatureArray
        }
    ];

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
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task InitializeAsync_EmptyApiInfos_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: EmptyApiInfoArray);
        var repository = new ApiInfoRepository(settings);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyFeaturesData))]
    public async Task GetByVersionAsync_NullOrEmptyFeatures_ReturnsEmptyFeatures(List<string>? features)
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: new List<ApiInfoSettings>
        {
            new ApiInfoSettings
            {
                Name = InMemoryModelSettingsFactory.DefaultApiName,
                Version = InMemoryModelSettingsFactory.DefaultApiVersion,
                Status = InMemoryModelSettingsFactory.DefaultApiStatus,
                Features = features!
            }
        });
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetByVersionAsync(InMemoryModelSettingsFactory.DefaultApiVersion, TestContext.Current.CancellationToken);

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
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetByVersionAsync(InMemoryModelSettingsFactory.DefaultApiVersion, TestContext.Current.CancellationToken);

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
        var settings = CreateValidSettings(apiInfos: CaseInsensitiveApiInfoArray);
        var repository = new ApiInfoRepository(settings);
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetByVersionAsync("v1", TestContext.Current.CancellationToken);

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
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.GetByVersionAsync("99.0", TestContext.Current.CancellationToken);

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
        var act = () => repository.GetByVersionAsync(ValidVersion, TestContext.Current.CancellationToken);

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
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => repository.GetByVersionAsync(version!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InitializeAsync_ApiInfoWithoutName_ThrowsArgumentException()
    {
        // Arrange
        var settings = CreateValidSettings(apiInfos: EmptyNameApiInfoArray);
        var repository = new ApiInfoRepository(settings);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    public static readonly TheoryData<List<string>?> NullOrEmptyFeaturesData = new()
    {
        { null },
        { new List<string>() }
    };

    private static InMemoryModelSettings CreateValidSettings(List<ApiInfoSettings>? apiInfos = null)
    {
        return InMemoryModelSettingsFactory.Create(apiInfos: apiInfos);
    }
}
