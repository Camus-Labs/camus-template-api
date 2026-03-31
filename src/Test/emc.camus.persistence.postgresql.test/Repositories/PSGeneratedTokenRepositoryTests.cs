using FluentAssertions;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class PSGeneratedTokenRepositoryTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();

    private PSGeneratedTokenRepository CreateRepository()
    {
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        return new PSGeneratedTokenRepository(unitOfWork);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        PSUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new PSGeneratedTokenRepository(unitOfWork!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_NullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.CreateAsync(null!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("generatedToken");
    }

    // --- GetPagedByCreatorUserIdAsync ---

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetPagedByCreatorUserIdAsync(Guid.Empty, null!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("pagination");
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_NullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.SaveAsync(null!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("generatedToken");
    }
}
