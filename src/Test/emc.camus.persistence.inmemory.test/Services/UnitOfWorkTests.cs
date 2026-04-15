using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.persistence.inmemory.Services;

namespace emc.camus.persistence.inmemory.test.Services;

public class UnitOfWorkTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new UnitOfWork(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- BeginTransactionAsync ---

    [Fact]
    public async Task BeginTransactionAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(NullLogger<UnitOfWork>.Instance);

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
        var unitOfWork = new UnitOfWork(NullLogger<UnitOfWork>.Instance);

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
        var unitOfWork = new UnitOfWork(NullLogger<UnitOfWork>.Instance);

        // Act
        var act = () => unitOfWork.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- CheckConnectivityAsync ---

    [Fact]
    public async Task CheckConnectivityAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(NullLogger<UnitOfWork>.Instance);

        // Act
        var act = () => unitOfWork.CheckConnectivityAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

}
