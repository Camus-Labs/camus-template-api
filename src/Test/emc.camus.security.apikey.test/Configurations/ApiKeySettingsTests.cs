using emc.camus.security.apikey.Configurations;
using FluentAssertions;

namespace emc.camus.security.apikey.test.Configurations;

/// <summary>
/// Unit tests for ApiKeySettings validation logic.
/// </summary>
public class ApiKeySettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new ApiKeySettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
