using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class OpenTelemetrySettingsTests
{
    private const string OtlpEndpoint = "http://collector:4317";

    // --- Validate ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    internal void Validate_ValidSettings_DoesNotThrow(OpenTelemetrySettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new OpenTelemetrySettings()];
        yield return [new OpenTelemetrySettings
        {
            Tracing = new TracingSettings
            {
                Exporter = TracingExporter.Otlp,
                OtlpEndpoint = OtlpEndpoint
            },
            Metrics = new MetricsSettings
            {
                Exporter = MetricsExporter.Otlp,
                OtlpEndpoint = OtlpEndpoint
            },
            Logs = new LogsSettings
            {
                Exporter = LogsExporter.Otlp,
                OtlpEndpoint = OtlpEndpoint
            }
        }];
    }

    [Theory]
    [MemberData(nameof(NullSubsettingsData))]
    internal void Validate_NullSubsettings_ThrowsInvalidOperationException(
        OpenTelemetrySettings settings, string expectedPattern)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedPattern);
    }

    public static IEnumerable<object[]> NullSubsettingsData()
    {
        yield return [new OpenTelemetrySettings { Tracing = null! }, "*Tracing*null*"];
        yield return [new OpenTelemetrySettings { Metrics = null! }, "*Metrics*null*"];
        yield return [new OpenTelemetrySettings { Logs = null! }, "*Logs*null*"];
    }

    [Theory]
    [MemberData(nameof(InvalidSubsettingsData))]
    internal void Validate_InvalidSubsettings_ThrowsInvalidOperationException(
        OpenTelemetrySettings settings, string expectedPattern)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedPattern);
    }

    public static IEnumerable<object[]> InvalidSubsettingsData()
    {
        yield return [new OpenTelemetrySettings { Tracing = new TracingSettings { Exporter = (TracingExporter)999 } }, "*Invalid*Exporter*"];
        yield return [new OpenTelemetrySettings { Metrics = new MetricsSettings { Exporter = (MetricsExporter)999 } }, "*Invalid*Exporter*"];
        yield return [new OpenTelemetrySettings { Logs = new LogsSettings { Exporter = (LogsExporter)999 } }, "*Invalid*Exporter*"];
    }
}
