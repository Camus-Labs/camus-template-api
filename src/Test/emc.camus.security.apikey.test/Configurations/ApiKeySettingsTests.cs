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
        var settings = new ApiKeySettings { SecretKeyName = "XApiKey" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptySecretKeyName_ThrowsArgumentException(string? secretKeyName)
    {
        // Arrange
        var settings = new ApiKeySettings { SecretKeyName = secretKeyName! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("SecretKeyName cannot be null or empty*")
            .And.ParamName.Should().Be("SecretKeyName");
    }
}
