using System.Diagnostics.Metrics;
using FluentAssertions;
using emc.camus.api.Metrics;

namespace emc.camus.api.test.Metrics;

public class IdempotencyMetricsTests : IDisposable
{
    private const string ServiceName = "test-service";

    private readonly IdempotencyMetrics _sut;

    public IdempotencyMetricsTests()
    {
        _sut = new IdempotencyMetrics(ServiceName);
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
        var act = () => new IdempotencyMetrics(serviceName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("serviceName");
    }

    [Fact]
    public void RecordCacheHit_ValidCall_RecordsMetric()
    {
        // Arrange
        var (listener, getValue) = CreateMeterListener("idempotency_cache_hit_total");
        using var _ = listener;

        // Act
        _sut.RecordCacheHit();

        // Assert
        getValue().Should().Be(1);
    }

    [Fact]
    public void RecordBodyConflict_ValidCall_RecordsMetric()
    {
        // Arrange
        var (listener, getValue) = CreateMeterListener("idempotency_body_conflict_total");
        using var _ = listener;

        // Act
        _sut.RecordBodyConflict();

        // Assert
        getValue().Should().Be(1);
    }

    [Fact]
    public void RecordCacheError_ValidCall_RecordsMetric()
    {
        // Arrange
        var (listener, getValue) = CreateMeterListener("idempotency_cache_error_total");
        using var _ = listener;

        // Act
        _sut.RecordCacheError();

        // Assert
        getValue().Should().Be(1);
    }

    private static (MeterListener Listener, Func<long> GetValue) CreateMeterListener(string instrumentName)
    {
        long recordedValue = 0;
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == instrumentName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            recordedValue = measurement;
        });
        listener.Start();
        return (listener, () => recordedValue);
    }
}
