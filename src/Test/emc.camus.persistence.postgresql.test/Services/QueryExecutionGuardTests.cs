using FluentAssertions;
using emc.camus.persistence.postgresql.Exceptions;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class QueryExecutionGuardTests
{
    // --- ExecuteAsync<T> ---

    [Fact]
    public async Task ExecuteAsync_Generic_SuccessfulOperation_ReturnsResult()
    {
        // Act
        var result = await QueryExecutionGuard.ExecuteAsync(() => Task.FromResult(42), "test-op");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_OperationThrows_ThrowsDatabaseQueryException()
    {
        // Arrange
        var innerException = new InvalidOperationException("db error");

        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync<int>(
            () => throw innerException, "test-op");

        // Assert
        (await act.Should().ThrowAsync<DatabaseQueryException>()
            .WithMessage("*test-op*"))
            .Which.InnerException.Should().Be(innerException);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_OperationCancelled_RethrowsWithoutWrapping()
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync<int>(
            () => throw new OperationCanceledException(), "test-op");

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_Generic_NullOperation_ThrowsArgumentNullException()
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync<int>(null!, "test-op");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(e => e.ParamName == "operation");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_Generic_InvalidOperationName_ThrowsArgumentException(string? operationName)
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync(() => Task.FromResult(1), operationName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "operationName");
    }

    // --- ExecuteAsync (void) ---

    [Fact]
    public async Task ExecuteAsync_Void_SuccessfulOperation_Completes()
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync(() => Task.CompletedTask, "test-op");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Void_OperationThrows_ThrowsDatabaseQueryException()
    {
        // Arrange
        var innerException = new InvalidOperationException("db error");

        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync(
            () => throw innerException, "test-op");

        // Assert
        (await act.Should().ThrowAsync<DatabaseQueryException>()
            .WithMessage("*test-op*"))
            .Which.InnerException.Should().Be(innerException);
    }

    [Fact]
    public async Task ExecuteAsync_Void_OperationCancelled_RethrowsWithoutWrapping()
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync(
            (Func<Task>)(() => throw new OperationCanceledException()), "test-op");

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_Void_NullOperation_ThrowsArgumentNullException()
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync((Func<Task>)null!, "test-op");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(e => e.ParamName == "operation");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_Void_InvalidOperationName_ThrowsArgumentException(string? operationName)
    {
        // Act
        var act = () => QueryExecutionGuard.ExecuteAsync(() => Task.CompletedTask, operationName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "operationName");
    }
}
