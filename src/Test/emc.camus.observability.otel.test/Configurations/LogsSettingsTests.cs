using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class LogsSettingsTests
{
    private const string InvalidUri = "not-a-valid-uri";

    // --- Validate ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    internal void Validate_ValidSettings_DoesNotThrow(LogsSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new LogsSettings()];
        yield return [new LogsSettings { Exporter = LogsExporter.Otlp }];
        yield return [new LogsSettings { Exporter = LogsExporter.Otlp, OtlpEndpoint = "http://collector:4317" }];
        yield return [new LogsSettings { Exporter = LogsExporter.Console, OtlpEndpoint = InvalidUri }];
    }

    [Fact]
    public void Validate_InvalidExporter_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings { Exporter = (LogsExporter)999 };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Exporter*");
    }

    [Fact]
    public void Validate_NullConsole_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings { Console = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Console*null*");
    }

    [Fact]
    public void Validate_InvalidConsoleSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new LogsSettings
        {
            Console = new ConsoleSettings { OutputTemplate = null! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutputTemplate*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_OtlpExporter_InvalidEndpoint_ThrowsInvalidOperationException(string? endpoint)
    {
        // Arrange
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Otlp,
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
        var settings = new LogsSettings
        {
            Exporter = LogsExporter.Otlp,
            OtlpEndpoint = InvalidUri
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OtlpEndpoint*valid*URI*");
    }
}
