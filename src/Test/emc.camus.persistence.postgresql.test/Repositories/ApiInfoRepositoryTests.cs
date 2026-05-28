using System.Data;
using System.Data.Common;
using FluentAssertions;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class ApiInfoRepositoryTests : IDisposable
{
    private const string ApiVersion = "1.0";
    private const string MissingVersion = "2.0";
    private const string ApiName = "TestApi";
    private const string ApiStatus = "active";
    private static readonly string[] ApiFeatures = new[] { "auth" };

    private readonly Mock<IConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IApiInfoDataAccess> _mockDataAccess;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly UnitOfWork _unitOfWork;

    public ApiInfoRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>();
        _mockDataAccess = new Mock<IApiInfoDataAccess>();
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

    private ApiInfoRepository CreateRepository()
    {
        return new ApiInfoRepository(_unitOfWork, new InitializationState(), _mockDataAccess.Object);
    }

    private ApiInfoRepository CreateInitializedRepository()
    {
        var initState = new InitializationState { ApiInfoRepositoryInitialized = true };
        return new ApiInfoRepository(_unitOfWork, initState, _mockDataAccess.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApiInfoRepository(null!, new InitializationState(), _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullInitState_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApiInfoRepository(_unitOfWork, null!, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("initState");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApiInfoRepository(_unitOfWork, new InitializationState(), null!);

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
    public async Task InitializeAsync_TableExists_SetsInitializedState()
    {
        // Arrange
        var initState = new InitializationState();
        var repository = new ApiInfoRepository(_unitOfWork, initState, _mockDataAccess.Object);
        _mockDataAccess
            .Setup(d => d.CheckTableExistsAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        initState.ApiInfoRepositoryInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_TableMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CheckTableExistsAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*api_info*");
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByVersionAsync(ApiVersion, TestContext.Current.CancellationToken);

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
        var repository = CreateInitializedRepository();

        // Act
        var act = () => repository.GetByVersionAsync(version!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "version");
    }

    [Fact]
    public async Task GetByVersionAsync_VersionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByVersionAsync(It.IsAny<IDbConnection>(), MissingVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(ApiInfoModel?));

        // Act
        var act = () => repository.GetByVersionAsync(MissingVersion, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{MissingVersion}*");
    }

    [Fact]
    public async Task GetByVersionAsync_VersionFound_ReturnsApiInfo()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByVersionAsync(It.IsAny<IDbConnection>(), ApiVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiInfoModel { Name = ApiName, Version = ApiVersion, Status = ApiStatus, Features = ApiFeatures });

        // Act
        var result = await repository.GetByVersionAsync(ApiVersion, TestContext.Current.CancellationToken);

        // Assert
        result.Name.Should().Be(ApiName);
        result.Version.Should().Be(ApiVersion);
        result.Status.Should().Be(ApiStatus);
    }
}
