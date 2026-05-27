using FluentAssertions;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.test.Configurations;

public class ConsoleSettingsTests
{
    // --- Validate ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    internal void Validate_ValidSettings_DoesNotThrow(ConsoleSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new ConsoleSettings()];
        yield return [new ConsoleSettings { OutputTemplate = "[{Timestamp:HH:mm:ss}] {Message}" }];
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidOutputTemplate_ThrowsInvalidOperationException(string? outputTemplate)
    {
        // Arrange
        var settings = new ConsoleSettings { OutputTemplate = outputTemplate! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutputTemplate*null*empty*");
    }
}
