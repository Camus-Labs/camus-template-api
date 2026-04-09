using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class RequestTimeoutSettingsTests
{
    // --- Validate: Valid Configuration ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new RequestTimeoutSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CustomValidValues_DoesNotThrow()
    {
        // Arrange
        var settings = new RequestTimeoutSettings
        {
            DefaultTimeoutSeconds = 15,
            TightTimeoutSeconds = 3,
            ExtendedTimeoutSeconds = 120
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: Invalid Values ---

    [Theory]
    [InlineData(nameof(RequestTimeoutSettings.DefaultTimeoutSeconds), 0)]
    [InlineData(nameof(RequestTimeoutSettings.DefaultTimeoutSeconds), -1)]
    [InlineData(nameof(RequestTimeoutSettings.DefaultTimeoutSeconds), 301)]
    [InlineData(nameof(RequestTimeoutSettings.TightTimeoutSeconds), 0)]
    [InlineData(nameof(RequestTimeoutSettings.TightTimeoutSeconds), -1)]
    [InlineData(nameof(RequestTimeoutSettings.TightTimeoutSeconds), 301)]
    [InlineData(nameof(RequestTimeoutSettings.ExtendedTimeoutSeconds), 0)]
    [InlineData(nameof(RequestTimeoutSettings.ExtendedTimeoutSeconds), -1)]
    [InlineData(nameof(RequestTimeoutSettings.ExtendedTimeoutSeconds), 301)]
    public void Validate_OutOfRangeValue_ThrowsInvalidOperationException(string propertyName, int value)
    {
        // Arrange
        var settings = new RequestTimeoutSettings();
        typeof(RequestTimeoutSettings).GetProperty(propertyName)!.SetValue(settings, value);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{propertyName}*");
    }

    // --- Default Values ---

    [Fact]
    public void DefaultValues_AreExpected()
    {
        // Arrange & Act
        var settings = new RequestTimeoutSettings();

        // Assert
        settings.DefaultTimeoutSeconds.Should().Be(30);
        settings.TightTimeoutSeconds.Should().Be(10);
        settings.ExtendedTimeoutSeconds.Should().Be(60);
    }
}
