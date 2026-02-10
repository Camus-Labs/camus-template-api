using emc.camus.documentation.swagger.Configurations;
using FluentAssertions;

namespace emc.camus.documentation.swagger.test.Configurations;

/// <summary>
/// Unit tests for SwaggerSettings configuration validation.
/// </summary>
public class SwaggerSettingsTests
{
    [Fact]
    public void Validate_WithValidEnabledSettings_ShouldNotThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "v1", Title = "API v1", Description = "Version 1" }
            },
            SecuritySchemes = new List<string> { "bearer" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithDisabledSwagger_ShouldNotRequireVersions()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = false,
            Versions = new List<ApiVersionInfo>(),
            SecuritySchemes = new List<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullVersions_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = null,
            SecuritySchemes = new List<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Versions cannot be null*")
            .WithParameterName("Versions");
    }

    [Fact]
    public void Validate_WithEnabledAndEmptyVersions_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>(),
            SecuritySchemes = new List<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one API version must be configured when Swagger is enabled*")
            .WithParameterName("Versions");
    }

    [Fact]
    public void Validate_WithNullVersionInList_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo> { null },
            SecuritySchemes = new List<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Versions cannot contain null values*")
            .WithParameterName("Versions");
    }

    [Fact]
    public void Validate_WithInvalidVersion_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "", Title = "API v1" }
            },
            SecuritySchemes = new List<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Version cannot be null or empty*");
    }

    [Fact]
    public void Validate_WithNullSecuritySchemes_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = false,
            Versions = new List<ApiVersionInfo>(),
            SecuritySchemes = null
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SecuritySchemes cannot be null*")
            .WithParameterName("SecuritySchemes");
    }

    [Theory]
    [InlineData("bearer")]
    [InlineData("apikey")]
    [InlineData("Bearer")]
    [InlineData("ApiKey")]
    public void Validate_WithValidSecurityScheme_ShouldNotThrow(string scheme)
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "v1", Title = "API v1" }
            },
            SecuritySchemes = new List<string> { scheme }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithInvalidSecurityScheme_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "v1", Title = "API v1" }
            },
            SecuritySchemes = new List<string> { "invalid-scheme" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid security scheme 'invalid-scheme'*")
            .WithParameterName("SecuritySchemes");
    }

    [Fact]
    public void Validate_WithEmptySecurityScheme_ShouldThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "v1", Title = "API v1" }
            },
            SecuritySchemes = new List<string> { "" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SecuritySchemes cannot contain null or empty values*")
            .WithParameterName("SecuritySchemes");
    }

    [Fact]
    public void Validate_WithMultipleValidSchemes_ShouldNotThrow()
    {
        // Arrange
        var settings = new SwaggerSettings
        {
            Enabled = true,
            Versions = new List<ApiVersionInfo>
            {
                new ApiVersionInfo { Version = "v1", Title = "API v1" }
            },
            SecuritySchemes = new List<string> { "bearer", "apikey" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }
}
