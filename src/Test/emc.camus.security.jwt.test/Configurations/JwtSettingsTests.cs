using FluentAssertions;
using emc.camus.security.jwt.Configurations;

namespace emc.camus.security.jwt.test.Configurations;

public class JwtSettingsTests
{
    // --- Defaults ---

    [Fact]
    public void Constructor_Defaults_HasExpectedValues()
    {
        // Arrange
        // Act
        var settings = new JwtSettings();

        // Assert
        settings.Issuer.Should().Be("https://auth.camus.com/");
        settings.Audience.Should().Be("https://app.camus.com/");
        settings.ExpirationMinutes.Should().Be(60);
        settings.RsaPrivateKeySecretName.Should().Be("RsaPrivateKeyPem");
        settings.Invoking(s => s.Validate()).Should().NotThrow();
    }

    // --- Validate Issuer ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrEmptyIssuer_ThrowsInvalidOperationException(string? issuer)
    {
        // Arrange
        var settings = new JwtSettings { Issuer = issuer! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Issuer*null*empty*");
    }

    [Fact]
    public void Validate_IssuerExceedingMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://" + new string('a', 193) // exceeds 200 chars
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Issuer*exceed*200*");
    }

    [Fact]
    public void Validate_IssuerAtMaxLength_DoesNotThrow()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "https://" + new string('a', 187) + ".com/" // exactly 200 chars
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_IssuerNotValidUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings { Issuer = "not-a-valid-url" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Issuer*valid*URL*");
    }

    // --- Validate Audience ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrEmptyAudience_ThrowsInvalidOperationException(string? audience)
    {
        // Arrange
        var settings = new JwtSettings { Audience = audience! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audience*null*empty*");
    }

    [Fact]
    public void Validate_AudienceExceedingMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Audience = "https://" + new string('a', 193) // exceeds 200 chars
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audience*exceed*200*");
    }

    [Fact]
    public void Validate_AudienceAtMaxLength_DoesNotThrow()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Audience = "https://" + new string('a', 187) + ".com/" // exactly 200 chars
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AudienceNotValidUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings { Audience = "not-a-valid-url" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audience*valid*URL*");
    }

    // --- Validate ExpirationMinutes ---

    [Theory]
    [InlineData(0)]
    [InlineData(43201)]
    public void Validate_ExpirationMinutesOutOfRange_ThrowsInvalidOperationException(int minutes)
    {
        // Arrange
        var settings = new JwtSettings { ExpirationMinutes = minutes };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExpirationMinutes*between*1*43200*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(43200)]
    public void Validate_ExpirationMinutesAtBoundary_DoesNotThrow(int minutes)
    {
        // Arrange
        var settings = new JwtSettings { ExpirationMinutes = minutes };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate RsaPrivateKeySecretName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrEmptyRsaPrivateKeySecretName_ThrowsInvalidOperationException(string? secretName)
    {
        // Arrange
        var settings = new JwtSettings { RsaPrivateKeySecretName = secretName! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RsaPrivateKeySecretName*null*empty*");
    }

    [Fact]
    public void Validate_RsaPrivateKeySecretNameExceedingMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new JwtSettings
        {
            RsaPrivateKeySecretName = new string('a', 51)
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RsaPrivateKeySecretName*exceed*50*");
    }

    [Fact]
    public void Validate_RsaPrivateKeySecretNameAtMaxLength_DoesNotThrow()
    {
        // Arrange
        var settings = new JwtSettings
        {
            RsaPrivateKeySecretName = new string('a', 50)
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

}
