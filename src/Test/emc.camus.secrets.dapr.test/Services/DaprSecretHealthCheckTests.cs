using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;
using emc.camus.secrets.dapr.test.Helpers;

namespace emc.camus.secrets.dapr.test.Services;

public class DaprSecretHealthCheckTests
{
    private const string ValidBaseHost = "localhost";
    private const string ValidHttpPort = "3500";
    private const string ValidSecretStoreName = "my-secret-store";
    private const int ValidTimeoutSeconds = 30;

    private static DaprSecretProviderSettings CreateValidSettings() => new()
    {
        BaseHost = ValidBaseHost,
        HttpPort = ValidHttpPort,
        SecretStoreName = ValidSecretStoreName,
        TimeoutSeconds = ValidTimeoutSeconds,
        SecretNames = new List<string> { "test-secret" }
    };

    private static DaprSecretProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new DaprSecretProvider(httpClient, CreateValidSettings());
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Arrange
        DaprSecretProvider? secretProvider = null;

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
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler);
        var healthCheck = new DaprSecretHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_SecretStoreUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var provider = CreateProvider(handler);
        var healthCheck = new DaprSecretHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Dapr secret store is unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ProviderThrowsException_ReturnsUnhealthyWithException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var provider = CreateProvider(handler);
        var healthCheck = new DaprSecretHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Dapr secret store is unreachable");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }
}
