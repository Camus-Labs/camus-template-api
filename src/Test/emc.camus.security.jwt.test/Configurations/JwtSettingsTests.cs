using emc.camus.security.jwt.Configurations;
using FluentAssertions;

namespace emc.camus.security.jwt.test.Configurations;

/// <summary>
/// Unit tests for JwtSettings to verify validation logic.
/// </summary>
public class JwtSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = "https://app.example.com/",
            ExpirationMinutes = 60
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]       // Null value
    [InlineData("")]         // Empty string
    [InlineData("   ")]      // Whitespace only
    public void Validate_WithNullOrEmptyIssuer_ThrowsArgumentException(string? issuer)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = issuer!,
            Audience = "https://app.example.com/",
            ExpirationMinutes = 60
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Issuer cannot be null or empty*")
            .And.ParamName.Should().Be("Issuer");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void Validate_WithInvalidIssuerUrl_ThrowsArgumentException(string issuer)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = issuer,
            Audience = "https://app.example.com/",
            ExpirationMinutes = 60
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Issuer must be a valid absolute URL: '{issuer}'*")
            .And.ParamName.Should().Be("Issuer");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyAudience_ThrowsArgumentException(string? audience)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = audience!,
            ExpirationMinutes = 60
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Audience cannot be null or empty*")
            .And.ParamName.Should().Be("Audience");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void Validate_WithInvalidAudienceUrl_ThrowsArgumentException(string audience)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = audience,
            ExpirationMinutes = 60
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Audience must be a valid absolute URL: '{audience}'*")
            .And.ParamName.Should().Be("Audience");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveExpirationMinutes_ThrowsArgumentException(int expirationMinutes)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = "https://app.example.com/",
            ExpirationMinutes = expirationMinutes
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ExpirationMinutes must be between 1 and 43200 (30 days)*")
            .And.ParamName.Should().Be("ExpirationMinutes");
    }

    [Theory]
    [InlineData(43201)]
    [InlineData(50000)]
    [InlineData(int.MaxValue)]
    public void Validate_WithExpirationMinutesExceedingMaximum_ThrowsArgumentException(int expirationMinutes)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = "https://app.example.com/",
            ExpirationMinutes = expirationMinutes
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ExpirationMinutes must be between 1 and 43200 (30 days)*")
            .And.ParamName.Should().Be("ExpirationMinutes");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(1440)]
    [InlineData(43200)]
    public void Validate_WithValidExpirationMinutes_DoesNotThrow(int expirationMinutes)
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://auth.example.com/",
            Audience = "https://app.example.com/",
            ExpirationMinutes = expirationMinutes
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
