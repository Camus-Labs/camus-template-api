using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Logging;

using emc.camus.api.Metrics;

namespace emc.camus.api.test.Metrics;

public class ErrorMetricsTests : IDisposable
{
    private const string ServiceName = "test-service";
    private const string ValidErrorCode = "test_error";
    private const int ValidHttpStatus = 500;
    private const string ValidPath = "/api/test";

    private readonly Mock<ILogger<ErrorMetrics>> _mockLogger;
    private readonly ErrorMetrics _sut;

    public ErrorMetricsTests()
    {
        _mockLogger = new Mock<ILogger<ErrorMetrics>>();
        _sut = new ErrorMetrics(ServiceName, _mockLogger.Object);
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
        var act = () => new ErrorMetrics(serviceName!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("serviceName");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ErrorMetrics(ServiceName, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordError_InvalidErrorCode_ThrowsArgumentException(string? errorCode)
    {
        // Act
        var act = () => _sut.RecordError(errorCode!, ValidHttpStatus, ValidPath);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("errorCode");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordError_InvalidHttpStatus_ThrowsArgumentOutOfRangeException(int httpStatus)
    {
        // Act
        var act = () => _sut.RecordError(ValidErrorCode, httpStatus, ValidPath);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("httpStatus");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordError_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act
        var act = () => _sut.RecordError(ValidErrorCode, ValidHttpStatus, path!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("path");
    }

    [Fact]
    public void RecordError_ValidInput_RecordsMetric()
    {
        // Arrange
        long recordedValue = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "error_responses_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            recordedValue = measurement;
        });
        listener.Start();

        // Act
        _sut.RecordError(ValidErrorCode, ValidHttpStatus, ValidPath);

        // Assert
        listener.RecordObservableInstruments();
        recordedValue.Should().Be(1);
    }

    [Fact]
    public void RecordError_CounterThrows_SuppressesExceptionAndLogs()
    {
        // Arrange - enable logging and register a listener that throws when a measurement is recorded
        var logger = new Mock<ILogger<ErrorMetrics>>();
        logger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
        using var metrics = new ErrorMetrics(ServiceName, logger.Object);

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "error_responses_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
            throw new InvalidOperationException("Simulated telemetry failure"));
        listener.Start();

        // Act
        var act = () => metrics.RecordError(ValidErrorCode, ValidHttpStatus, ValidPath);

        // Assert
        act.Should().NotThrow();
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("error metrics")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorMetrics>>();
        var metrics = new ErrorMetrics(ServiceName, logger.Object);

        // Act
        var act = () =>
        {
            metrics.Dispose();
            metrics.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }
}
