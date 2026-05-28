using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using emc.camus.application.Secrets;
using emc.camus.secrets.dapr.Services;

namespace emc.camus.secrets.dapr.test.Services;

public class DaprSecretHealthCheckTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Arrange
        ISecretProvider? secretProvider = null;

        // Act
        var act = () => new DaprSecretHealthCheck(secretProvider!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("secretProvider");
    }

    // --- CheckHealthAsync ---

    [Fact]
    public async Task CheckHealthAsync_SecretStoreReachable_ReturnsHealthy()
    {
        // Arrange
        var mockProvider = new Mock<ISecretProvider>();
        mockProvider.Setup(p => p.CheckConnectivityAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var healthCheck = new DaprSecretHealthCheck(mockProvider.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Dapr secret store is reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_SecretStoreUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        var mockProvider = new Mock<ISecretProvider>();
        mockProvider.Setup(p => p.CheckConnectivityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store unreachable"));
        var healthCheck = new DaprSecretHealthCheck(mockProvider.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Dapr secret store is unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ProviderThrowsException_ReturnsUnhealthyWithException()
    {
        // Arrange
        var mockProvider = new Mock<ISecretProvider>();
        mockProvider.Setup(p => p.CheckConnectivityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var healthCheck = new DaprSecretHealthCheck(mockProvider.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Dapr secret store is unreachable");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }
}
