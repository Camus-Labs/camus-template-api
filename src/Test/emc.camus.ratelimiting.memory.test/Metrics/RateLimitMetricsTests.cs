using emc.camus.ratelimiting.memory.Metrics;
using FluentAssertions;
using System.Diagnostics.Metrics;

namespace emc.camus.ratelimiting.memory.test.Metrics;

/// <summary>
/// Unit tests for RateLimitMetrics to verify metric recording logic.
/// </summary>
public class RateLimitMetricsTests
{
    private const string TestServiceName = "test-service";

    [Fact]
    public void Constructor_ShouldInitializeMetrics()
    {
        // Act
        var metrics = new RateLimitMetrics(TestServiceName);

        // Assert
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void RecordHit_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordHit("default", "/api/test", "GET");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRejection_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordRejection("default", "/api/test", "GET", "192.168.1.1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHit_WithNullPolicy_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordHit(null!, "/api/test", "GET");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRejection_WithNullPolicy_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordRejection(null!, "/api/test", "GET", "192.168.1.1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHit_WithEmptyEndpoint_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordHit("default", string.Empty, "GET");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRejection_WithEmptyEndpoint_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordRejection("default", string.Empty, "GET", "user123");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("default", "/api/users", "GET")]
    [InlineData("strict", "/api/auth/token", "POST")]
    [InlineData("relaxed", "/api/info", "GET")]
    public void RecordHit_WithVariousPolicies_ShouldNotThrow(string policy, string endpoint, string method)
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordHit(policy, endpoint, method);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("default", "/api/users", "GET")]
    [InlineData("strict", "/api/auth/token", "POST")]
    [InlineData("relaxed", "/api/info", "GET")]
    public void RecordRejection_WithVariousPolicies_ShouldNotThrow(string policy, string endpoint, string method)
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordRejection(policy, endpoint, method, "192.168.1.1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHit_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var act = () => metrics.RecordHit("default", "/api/test", "GET");
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void RecordRejection_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var act = () => metrics.RecordRejection("strict", "/api/test", "POST", "user123");
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void RecordHit_AndRecordRejection_ShouldBothWork()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var actHit = () => metrics.RecordHit("default", "/api/test", "GET");
        var actRejection = () => metrics.RecordRejection("default", "/api/test", "GET", "192.168.1.1");

        // Assert
        actHit.Should().NotThrow();
        actRejection.Should().NotThrow();
    }

    [Fact]
    public void RecordUndefinedPolicy_ShouldNotThrow()
    {
        // Arrange
        var metrics = new RateLimitMetrics(TestServiceName);

        // Act
        var act = () => metrics.RecordUndefinedPolicy("nonexistent-policy", "/api/test");

        // Assert
        act.Should().NotThrow();
    }
}
