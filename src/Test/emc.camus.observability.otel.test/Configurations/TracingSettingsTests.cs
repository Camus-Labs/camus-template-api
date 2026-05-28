using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class TracingSettingsTests
{
    private const string InvalidUri = "not-a-valid-uri";

    // --- Validate ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    internal void Validate_ValidSettings_DoesNotThrow(TracingSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new TracingSettings()];
        yield return [new TracingSettings { Exporter = TracingExporter.Otlp }];
        yield return [new TracingSettings { Exporter = TracingExporter.Otlp, OtlpEndpoint = "http://collector:4317" }];
        yield return [new TracingSettings { Exporter = TracingExporter.Console, OtlpEndpoint = InvalidUri }];
    }

    [Fact]
    public void Validate_InvalidExporter_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new TracingSettings { Exporter = (TracingExporter)999 };

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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
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
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
            OtlpEndpoint = InvalidUri
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*valid*URI*");
    }
}
