using FluentAssertions;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class PSUserRepositoryTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();

    private PSUserRepository CreateRepository()
    {
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        return new PSUserRepository(unitOfWork, new PSInitializationState());
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        PSUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new PSUserRepository(unitOfWork!, new PSInitializationState());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- ValidateCredentialsAsync ---

    [Fact]
    public async Task ValidateCredentialsAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.ValidateCredentialsAsync("user", "pass");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByIdAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    // --- UpdateLastLoginAsync ---

    [Fact]
    public async Task UpdateLastLoginAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.UpdateLastLoginAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }
}
