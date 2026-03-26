using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.persistence.postgresql.Repositories;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class PSApiInfoRepositoryTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();

    // --- Constructor ---

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IConnectionFactory? connectionFactory = null;

        // Act
        var act = () => new PSApiInfoRepository(connectionFactory!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connectionFactory");
    }

    // --- GetByVersionAsync ---

    [Fact]
    public async Task GetByVersionAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new PSApiInfoRepository(_mockConnectionFactory.Object);

        // Act
        var act = () => repository.GetByVersionAsync("1.0");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }
}
