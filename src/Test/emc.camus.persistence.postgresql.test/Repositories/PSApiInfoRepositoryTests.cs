using FluentAssertions;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class PSApiInfoRepositoryTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();

    private PSApiInfoRepository CreateRepository()
    {
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        return new PSApiInfoRepository(unitOfWork, new PSInitializationState());
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        PSUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new PSApiInfoRepository(unitOfWork!, new PSInitializationState());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

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
    public async Task GetByVersionAsync_Initialized_InvalidVersion_ThrowsArgumentException(string? version)
    {
        // Arrange
        var initState = new PSInitializationState { ApiInfoRepositoryInitialized = true };
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        var repository = new PSApiInfoRepository(unitOfWork, initState);

        // Act
        var act = () => repository.GetByVersionAsync(version!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "version");
    }
}
