using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class TracingSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new TracingSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(TracingExporter.None)]
    [InlineData(TracingExporter.Console)]
    [InlineData(TracingExporter.Otlp)]
    public void Validate_DefinedExporter_DoesNotThrow(TracingExporter exporter)
    {
        // Arrange
        var settings = new TracingSettings { Exporter = exporter };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
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

    [Fact]
    public void Validate_OtlpExporter_ValidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Otlp,
            OtlpEndpoint = "http://collector:4317"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
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
            OtlpEndpoint = "not-a-valid-uri"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*valid*URI*");
    }

    [Fact]
    public void Validate_NonOtlpExporter_InvalidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new TracingSettings
        {
            Exporter = TracingExporter.Console,
            OtlpEndpoint = "not-a-valid-uri"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
