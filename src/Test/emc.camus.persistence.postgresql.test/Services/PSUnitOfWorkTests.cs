using System.Data;
using System.Data.Common;
using FluentAssertions;
using Moq.Protected;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class PSUnitOfWorkTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<DbConnection> _mockConnection = new();
    private readonly Mock<DbTransaction> _mockTransaction = new();

    // --- Constructor ---

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IConnectionFactory? connectionFactory = null;

        // Act
        var act = () => new PSUnitOfWork(connectionFactory!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connectionFactory");
    }

    // --- GetConnectionAsync ---

    [Fact]
    public async Task GetConnectionAsync_FirstCall_ReturnsConnection()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        var connection = await unitOfWork.GetConnectionAsync();

        // Assert
        connection.Should().BeSameAs(_mockConnection.Object);
    }

    [Fact]
    public async Task GetConnectionAsync_MultipleCalls_CreatesConnectionOnlyOnce()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        await unitOfWork.GetConnectionAsync();
        await unitOfWork.GetConnectionAsync();

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- BeginTransactionAsync ---

    [Fact]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>("BeginDbTransactionAsync",
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        await unitOfWork.BeginTransactionAsync();

        // Assert
        _mockConnection.Protected()
            .Verify<ValueTask<DbTransaction>>("BeginDbTransactionAsync", Times.Once(),
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>());
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_WithTransaction_CommitsTransaction()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>("BeginDbTransactionAsync",
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync();

        // Act
        await unitOfWork.CommitAsync();

        // Assert
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WithoutTransaction_DoesNotThrow()
    {
        // Arrange
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => unitOfWork.CommitAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_WithTransaction_RollsBackTransaction()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>("BeginDbTransactionAsync",
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync();

        // Act
        await unitOfWork.RollbackAsync();

        // Assert
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_WithoutTransaction_DoesNotThrow()
    {
        // Arrange
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => unitOfWork.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- Dispose ---

    [Fact]
    public async Task Dispose_WithConnectionAndTransaction_DisposesResources()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>("BeginDbTransactionAsync",
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync();

        // Act
        unitOfWork.Dispose();

        // Assert
        _mockTransaction.Protected().Verify("Dispose", Times.Once(), ItExpr.IsAny<bool>());
        _mockConnection.Protected().Verify("Dispose", Times.Once(), ItExpr.IsAny<bool>());
    }

    [Fact]
    public void Dispose_WithoutConnection_DoesNotThrow()
    {
        // Arrange
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => unitOfWork.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    // --- DisposeAsync ---

    [Fact]
    public async Task DisposeAsync_WithConnectionAndTransaction_DisposesResources()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>("BeginDbTransactionAsync",
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        var unitOfWork = new PSUnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync();

        // Act
        await unitOfWork.DisposeAsync();

        // Assert
        _mockTransaction.Verify(t => t.DisposeAsync(), Times.Once);
        _mockConnection.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
