using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class MetricsSettingsTests
{
    private const string InvalidUri = "not-a-valid-uri";
    private const string MetricName = "http.server.duration";
    private const string MeterName = ".infrastructure";
    private static readonly string[] ValidDisabledMetrics = [MetricName, "http.server.request.duration"];
    private static readonly string[] ValidDisabledMeters = [MeterName, ".business"];
    private static readonly string[] DisabledMetricsWithNull = [MetricName, null!];
    private static readonly string[] DisabledMetricsWithEmpty = [MetricName, ""];
    private static readonly string[] DisabledMetricsWithWhitespace = [MetricName, "   "];
    private static readonly string[] DisabledMetersWithNull = [MeterName, null!];
    private static readonly string[] DisabledMetersWithEmpty = [MeterName, ""];
    private static readonly string[] DisabledMetersWithWhitespace = [MeterName, "   "];

    // --- Validate ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    internal void Validate_ValidSettings_DoesNotThrow(MetricsSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new MetricsSettings()];
        yield return [new MetricsSettings { Exporter = MetricsExporter.Otlp }];
        yield return [new MetricsSettings { Exporter = MetricsExporter.Otlp, OtlpEndpoint = "http://collector:4317" }];
        yield return [new MetricsSettings { Exporter = MetricsExporter.Console, OtlpEndpoint = InvalidUri }];
    }

    [Fact]
    public void Validate_InvalidExporter_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new MetricsSettings { Exporter = (MetricsExporter)999 };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_OtlpExporter_InvalidEndpoint_ThrowsInvalidOperationException(string? endpoint)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
            OtlpEndpoint = endpoint!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*null*empty*");
    }

    [Fact]
    public void Validate_OtlpExporter_InvalidUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
            OtlpEndpoint = InvalidUri
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*valid*URI*");
    }

    [Theory]
    [MemberData(nameof(NullCollectionData))]
    internal void Validate_NullCollection_ThrowsInvalidOperationException(
        MetricsSettings settings, string expectedPattern)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedPattern);
    }

    public static IEnumerable<object[]> NullCollectionData()
    {
        yield return [new MetricsSettings { DisabledMetrics = null! }, "*DisabledMetrics*null*"];
        yield return [new MetricsSettings { DisabledMeters = null! }, "*DisabledMeters*null*"];
    }

    [Theory]
    [MemberData(nameof(InvalidDisabledMetricsData))]
    internal void Validate_DisabledMetricsWithInvalidEntry_ThrowsInvalidOperationException(string[] disabledMetrics)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMetrics = disabledMetrics
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMetrics*null*empty*");
    }

    public static IEnumerable<object[]> InvalidDisabledMetricsData()
    {
        yield return [DisabledMetricsWithNull];
        yield return [DisabledMetricsWithEmpty];
        yield return [DisabledMetricsWithWhitespace];
    }

    [Theory]
    [MemberData(nameof(InvalidDisabledMetersData))]
    internal void Validate_DisabledMetersWithInvalidEntry_ThrowsInvalidOperationException(string[] disabledMeters)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMeters = disabledMeters
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMeters*null*empty*");
    }

    public static IEnumerable<object[]> InvalidDisabledMetersData()
    {
        yield return [DisabledMetersWithNull];
        yield return [DisabledMetersWithEmpty];
        yield return [DisabledMetersWithWhitespace];
    }

    [Theory]
    [MemberData(nameof(PopulatedCollectionData))]
    internal void Validate_PopulatedCollection_DoesNotThrow(MetricsSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> PopulatedCollectionData()
    {
        yield return [new MetricsSettings { DisabledMetrics = ValidDisabledMetrics }];
        yield return [new MetricsSettings { DisabledMeters = ValidDisabledMeters }];
    }
}
