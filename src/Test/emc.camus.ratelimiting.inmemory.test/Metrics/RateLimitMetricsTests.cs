using System.Diagnostics.Metrics;
using FluentAssertions;
using emc.camus.ratelimiting.inmemory.Metrics;
using emc.camus.ratelimiting.inmemory.test.Helpers;

namespace emc.camus.ratelimiting.inmemory.test.Metrics;

public class RateLimitMetricsTests : IDisposable
{
    private const string ValidPolicyName = "default";
    private const string ValidMethod = "GET";

    private readonly RateLimitMetrics _sut;

    public RateLimitMetricsTests()
    {
        _sut = new RateLimitMetrics("test-service");
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidServiceName_ThrowsArgumentException(string? serviceName)
    {
        // Act
        var act = () => new RateLimitMetrics(serviceName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("serviceName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordRejection_InvalidPolicyName_ThrowsArgumentException(string? policyName)
    {
        // Act
        var act = () => _sut.RecordRejection(policyName!, ValidMethod);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("policyName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordRejection_InvalidMethod_ThrowsArgumentException(string? method)
    {
        // Act
        var act = () => _sut.RecordRejection(ValidPolicyName, method!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("method");
    }

    [Fact]
    public void RecordRejection_ValidParameters_RecordsMetric()
    {
        // Arrange
        var (listener, getValue) = MeterCaptureBuilder.CreateListener("rate_limit_rejections_total");
        using var _ = listener;

        // Act
        _sut.RecordRejection(ValidPolicyName, ValidMethod);

        // Assert
        getValue().Should().Be(1);
    }

}
