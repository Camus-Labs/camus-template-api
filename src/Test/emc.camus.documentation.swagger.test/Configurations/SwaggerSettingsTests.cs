using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.documentation.swagger.Configurations;

namespace emc.camus.documentation.swagger.test.Configurations;

public class SwaggerSettingsTests
{
    // --- Validate (valid settings) ---

    [Fact]
    public void Validate_DisabledWithNoVersions_DoesNotThrow()
    {
        // Arrange
        var settings = new SwaggerSettings { Enabled = false };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EnabledWithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidEnabledSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EnabledWithEmptySecuritySchemes_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidEnabledSettings();
        settings.SecuritySchemes = new List<string>();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Versions validation ---

    [Fact]
    public void Validate_NullVersions_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new SwaggerSettings { Versions = null! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Versions*null*");
    }

    [Fact]
    public void Validate_EnabledWithEmptyVersions_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionSettings>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*version*configured*");
    }

    [Fact]
    public void Validate_EnabledWithNullVersionEntry_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionSettings> { null! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*null*values*");
    }

    [Fact]
    public void Validate_EnabledWithInvalidVersionEntry_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionSettings>
            {
                new() { Version = "", Title = "Test", Description = "Desc" }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Version*null*empty*");
    }

    // --- SecuritySchemes validation ---

    [Fact]
    public void Validate_NullSecuritySchemes_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = false,
            SecuritySchemes = null!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecuritySchemes*null*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EnabledWithEmptyOrNullSchemeEntry_ThrowsInvalidOperationException(string? scheme)
    {
        // Arrange
        var settings = CreateValidEnabledSettings();
        settings.SecuritySchemes = new List<string> { scheme! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecuritySchemes*null*empty*");
    }

    [Fact]
    public void Validate_EnabledWithInvalidScheme_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidEnabledSettings();
        settings.SecuritySchemes = new List<string> { "InvalidScheme" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid security scheme*InvalidScheme*");
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("ApiKey")]
    [InlineData("bearer")]
    public void Validate_EnabledWithValidScheme_DoesNotThrow(string scheme)
    {
        // Arrange
        var settings = CreateValidEnabledSettings();
        settings.SecuritySchemes = new List<string> { scheme };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EnabledWithMultipleValidSchemes_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidEnabledSettings();
        settings.SecuritySchemes = new List<string>
        {
            AuthenticationSchemes.JwtBearer,
            AuthenticationSchemes.ApiKey
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_DisabledWithInvalidSchemes_DoesNotThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = false,
            SecuritySchemes = new List<string> { "InvalidScheme" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }



    private static SwaggerSettings CreateValidEnabledSettings()
    {
        return new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionSettings>
            {
                new()
                {
                    Version = "v1",
                    Title = "Test API",
                    Description = "Test API Description"
                }
            },
            SecuritySchemes = new List<string> { AuthenticationSchemes.JwtBearer }
        };
    }
}
