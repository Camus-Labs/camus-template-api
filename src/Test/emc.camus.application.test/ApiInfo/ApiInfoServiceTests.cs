using FluentAssertions;
using Moq;
using emc.camus.application.ApiInfo;
using emc.camus.domain.Auth;

namespace emc.camus.application.test.ApiInfo;

public class ApiInfoServiceTests
{
    private const string ValidVersion = "1.0";
    private const string ValidStatus = "Available";
    private static readonly List<string> ValidFeatures = ["Auth", "Tokens"];

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
        var apiInfo = new emc.camus.domain.Auth.ApiInfo(ValidVersion, ValidStatus, ValidFeatures);

        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion)).ReturnsAsync(apiInfo);

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
        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion))
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
        _repositoryMock.Setup(r => r.GetByVersionAsync(ValidVersion))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService();

        // Act
        var act = () => service.GetByVersionAsync(filter);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to retrieve API information*system error*");
    }

    // --- Initialize ---

    [Fact]
    public void Initialize_Success_CallsRepositoryInitialize()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Initialize();

        // Assert
        _repositoryMock.Verify(r => r.Initialize(), Times.Once);
    }

    [Fact]
    public void Initialize_RepositoryFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.Initialize()).Throws(new InvalidOperationException("Connection failed"));
        var service = CreateService();

        // Act
        var act = () => service.Initialize();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to initialize*API info*");
    }
}
