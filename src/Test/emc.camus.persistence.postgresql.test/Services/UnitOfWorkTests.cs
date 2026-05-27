using System.Data;
using System.Data.Common;
using FluentAssertions;
using Moq.Protected;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class UnitOfWorkTests : IDisposable
{
    private const string BeginDbTransactionAsyncMethod = "BeginDbTransactionAsync";

    private readonly Mock<IConnectionFactory> _mockConnectionFactory;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly Mock<DbTransaction> _mockTransaction;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>();
        _mockConnection = new Mock<DbConnection>();
        _mockTransaction = new Mock<DbTransaction>();

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

    // --- Constructor ---

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IConnectionFactory? connectionFactory = null;

        // Act
        var act = () => new UnitOfWork(connectionFactory!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connectionFactory");
    }

    // --- CheckConnectivityAsync ---

    [Fact]
    public async Task CheckConnectivityAsync_FirstCall_CreatesConnection()
    {
        // Act
        await _unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckConnectivityAsync_MultipleCalls_CreatesConnectionOnlyOnce()
    {
        // Arrange
        await _unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- BeginTransactionAsync ---

    [Fact]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Arrange
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod,
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));

        // Act
        await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockConnection.Protected()
            .Verify<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod, Times.Once(),
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>());
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_WithTransaction_CommitsTransaction()
    {
        // Arrange
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod,
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_WithoutTransaction_DoesNotThrow()
    {
        // Act
        var act = () => _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_WithTransaction_RollsBackTransaction()
    {
        // Arrange
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod,
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_WithoutTransaction_DoesNotThrow()
    {
        // Act
        var act = () => _unitOfWork.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- Dispose ---

    [Fact]
    public async Task Dispose_WithConnectionAndTransaction_DisposesResources()
    {
        // Arrange
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod,
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        using var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        unitOfWork.Dispose();

        // Assert
        _mockTransaction.Protected().Verify("Dispose", Times.Once(), ItExpr.IsAny<bool>());
        _mockConnection.Protected().Verify("Dispose", Times.Once(), ItExpr.IsAny<bool>());
    }

    [Fact]
    public void Dispose_WithoutConnection_DoesNotThrow()
    {
        // Act
        var act = () => _unitOfWork.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    // --- DisposeAsync ---

    [Fact]
    public async Task DisposeAsync_WithConnectionAndTransaction_DisposesResources()
    {
        // Arrange
        _mockConnection.Protected()
            .Setup<ValueTask<DbTransaction>>(BeginDbTransactionAsyncMethod,
                ItExpr.IsAny<IsolationLevel>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(new ValueTask<DbTransaction>(_mockTransaction.Object));
        await using var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        await unitOfWork.DisposeAsync();

        // Assert
        _mockTransaction.Verify(t => t.DisposeAsync(), Times.Once);
        _mockConnection.Verify(c => c.DisposeAsync(), Times.Once);
    }
}
