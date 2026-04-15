using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class HealthCheckTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        IUnitOfWork? unitOfWork = null;

        // Act
        var act = () => new HealthCheck(unitOfWork!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- CheckHealthAsync ---

    [Fact]
    public async Task CheckHealthAsync_DatabaseReachable_ReturnsHealthy()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.CheckConnectivityAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var healthCheck = new HealthCheck(_mockUnitOfWork.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DatabaseUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.CheckConnectivityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));
        var healthCheck = new HealthCheck(_mockUnitOfWork.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("PostgreSQL database is unreachable");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }
}
