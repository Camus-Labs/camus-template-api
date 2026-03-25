using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class MetricsSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(MetricsExporter.None)]
    [InlineData(MetricsExporter.Console)]
    [InlineData(MetricsExporter.Otlp)]
    public void Validate_DefinedExporter_DoesNotThrow(MetricsExporter exporter)
    {
        // Arrange
        var settings = new MetricsSettings { Exporter = exporter };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
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

    [Fact]
    public void Validate_OtlpExporter_ValidEndpoint_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Otlp,
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
        var settings = new MetricsSettings
        {
            Exporter = MetricsExporter.Console,
            OtlpEndpoint = "not-a-valid-uri"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullDisabledMetrics_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new MetricsSettings { DisabledMetrics = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMetrics*null*");
    }

    [Fact]
    public void Validate_NullDisabledMeters_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new MetricsSettings { DisabledMeters = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMeters*null*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DisabledMetricsWithInvalidEntry_ThrowsInvalidOperationException(string? entry)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMetrics = new[] { "http.server.duration", entry! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMetrics*null*empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_DisabledMetersWithInvalidEntry_ThrowsInvalidOperationException(string? entry)
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMeters = new[] { ".infrastructure", entry! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DisabledMeters*null*empty*");
    }

    [Fact]
    public void Validate_PopulatedDisabledMetrics_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMetrics = new[] { "http.server.duration", "http.server.request.duration" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_PopulatedDisabledMeters_DoesNotThrow()
    {
        // Arrange
        var settings = new MetricsSettings
        {
            DisabledMeters = new[] { ".infrastructure", ".business" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
