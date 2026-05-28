using System.Diagnostics.Metrics;
using FluentAssertions;
using emc.camus.api.Metrics;
using emc.camus.api.test.Helpers;

namespace emc.camus.api.test.Metrics;

public class RateLimitMetricsTests : IDisposable
{
    private const string ServiceName = "test-service";
    private const string ValidPolicyName = "default";
    private const string ValidMethod = "GET";

    private readonly RateLimitMetrics _sut;

    public RateLimitMetricsTests()
    {
        _sut = new RateLimitMetrics(ServiceName);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Constructor ---

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

    // --- AC-05: RecordRejection emits metric with correct name ---

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
    public void RecordRejection_ValidParameters_RecordsMetricWithCorrectName()
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
