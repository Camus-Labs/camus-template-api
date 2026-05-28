using FluentAssertions;
using emc.camus.security.apikey.Configurations;

namespace emc.camus.security.apikey.test.Configurations;

public class ApiKeySettingsTests
{
    // --- Validate (valid settings) ---

    [Theory]
    [InlineData("MySecret")]
    [InlineData("XApiKey")]
    public void Validate_ValidApiKeySecretName_DoesNotThrow(string secretName)
    {
        // Arrange
        var settings = new ApiKeySettings { ApiKeySecretName = secretName };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- ApiKeySecretName validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidApiKeySecretName_ThrowsInvalidOperationException(string? secretName)
    {
        // Arrange
        var settings = new ApiKeySettings { ApiKeySecretName = secretName! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiKeySecretName*null*empty*");
    }
}
