using System.Data.Common;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class PSHealthCheckTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<DbConnection> _mockConnection = new();

    private PSUnitOfWork CreateUnitOfWork()
    {
        return new PSUnitOfWork(_mockConnectionFactory.Object);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        PSUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new PSHealthCheck(unitOfWork!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- CheckHealthAsync ---

    [Fact]
    public async Task CheckHealthAsync_DatabaseReachable_ReturnsHealthy()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockConnection.Object);
        var unitOfWork = CreateUnitOfWork();
        var healthCheck = new PSHealthCheck(unitOfWork);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DatabaseUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));
        var unitOfWork = CreateUnitOfWork();
        var healthCheck = new PSHealthCheck(unitOfWork);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("PostgreSQL database is unreachable");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }
}
