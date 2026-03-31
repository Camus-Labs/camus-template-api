using System.Linq;
using FluentAssertions;
using Moq;
using emc.camus.application.ApiInfo;
using emc.camus.domain.Auth;

namespace emc.camus.application.test.ApiInfo;

public class ApiInfoServiceTests
{
    private const string ValidVersion = "1.0";
    private const string ValidStatus = "Available";
    private static readonly IReadOnlyList<string> ValidFeatures = ["Auth", "Tokens"];

    private readonly Mock<IApiInfoRepository> _repositoryMock = new();

    private ApiInfoService CreateService()
    {
        return new ApiInfoService(_repositoryMock.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidRepository_CreatesInstance()
    {
        // Arrange
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new ApiInfoService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("repository");
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_ValidFilter_ReturnsDetailView()
    {
        // Arrange
        var filter = new ApiInfoFilter(ValidVersion);
        var apiInfo = new emc.camus.domain.Auth.ApiInfo(ValidVersion, ValidStatus, ValidFeatures.ToList());

        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion, It.IsAny<CancellationToken>())).ReturnsAsync(apiInfo);

        var service = CreateService();

        // Act
        var result = await service.GetByVersionAsync(filter);

        // Assert
        result.Version.Should().Be(ValidVersion);
        result.Status.Should().Be(ValidStatus);
        result.Features.Should().BeEquivalentTo(ValidFeatures);
    }

    [Fact]
    public async Task GetByVersionAsync_NullFilter_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.GetByVersionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("filter");
    }

    [Fact]
    public async Task GetByVersionAsync_VersionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var filter = new ApiInfoFilter(ValidVersion);
        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Version not found"));

        var service = CreateService();

        // Act
        var act = () => service.GetByVersionAsync(filter);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByVersionAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var filter = new ApiInfoFilter(ValidVersion);
        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService();

        // Act
        var act = () => service.GetByVersionAsync(filter);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to retrieve API information*system error*");
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_Success_CallsRepositoryInitializeAsync()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        _repositoryMock.Verify(r => r.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_RepositoryFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.InitializeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Connection failed"));
        var service = CreateService();

        // Act
        var act = () => service.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to initialize*API info*");
    }
}
