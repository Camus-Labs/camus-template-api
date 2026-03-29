using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.persistence.inmemory.Services;

namespace emc.camus.persistence.inmemory.test.Services;

public class IMUnitOfWorkTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new IMUnitOfWork(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- BeginTransactionAsync ---

    [Fact]
    public async Task BeginTransactionAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new IMUnitOfWork(NullLogger<IMUnitOfWork>.Instance);

        // Act
        var act = () => unitOfWork.BeginTransactionAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new IMUnitOfWork(NullLogger<IMUnitOfWork>.Instance);

        // Act
        var act = () => unitOfWork.CommitAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new IMUnitOfWork(NullLogger<IMUnitOfWork>.Instance);

        // Act
        var act = () => unitOfWork.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

}
