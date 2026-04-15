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
    private static readonly string[] ApiFeatures = new[] { "auth" };

    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IApiInfoDataAccess> _mockDataAccess = new();
    private readonly Mock<DbConnection> _mockConnection = new();
    private readonly UnitOfWork _unitOfWork;

    public ApiInfoRepositoryTests()
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
        // Arrange
        UnitOfWork? unitOfWork = null;

        // Act
        var act = () => new ApiInfoRepository(unitOfWork!, new InitializationState(), _mockDataAccess.Object);

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
        var act = () => new ApiInfoRepository(unitOfWork, null!, _mockDataAccess.Object);

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
        var act = () => new ApiInfoRepository(unitOfWork, new InitializationState(), null!);

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
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CheckTableExistsAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await repository.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        var act = () => repository.InitializeAsync(TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
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
        var act = () => repository.GetByVersionAsync("1.0", TestContext.Current.CancellationToken);

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
            .Setup(d => d.FindByVersionAsync(It.IsAny<IDbConnection>(), "2.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiInfoModel?)null);

        // Act
        var act = () => repository.GetByVersionAsync("2.0", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*2.0*");
    }

    [Fact]
    public async Task GetByVersionAsync_VersionFound_ReturnsApiInfo()
    {
        // Arrange
        var repository = CreateInitializedRepository();
        _mockDataAccess
            .Setup(d => d.FindByVersionAsync(It.IsAny<IDbConnection>(), "1.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiInfoModel { Name = "TestApi", Version = "1.0", Status = "active", Features = ApiFeatures });

        // Act
        var result = await repository.GetByVersionAsync("1.0", TestContext.Current.CancellationToken);

        // Assert
        result.Name.Should().Be("TestApi");
        result.Version.Should().Be("1.0");
        result.Status.Should().Be("active");
    }
}
