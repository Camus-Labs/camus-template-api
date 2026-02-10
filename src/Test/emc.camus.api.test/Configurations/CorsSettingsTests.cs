using emc.camus.api.Configurations;
using emc.camus.application.Generic;
using FluentAssertions;
using Microsoft.Net.Http.Headers;

namespace emc.camus.api.test.Configurations;

/// <summary>
/// Unit tests for CorsSettings validation logic.
/// </summary>
public class CorsSettingsTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedMethods = new[] { "GET", "POST" },
            AllowedHeaders = new[] { "Content-Type" },
            ExposedHeaders = new[] { Headers.TraceId },
            AllowCredentials = false,
            PreflightMaxAgeMinutes = 60
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyPolicyName_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "",
            AllowedOrigins = new[] { "https://example.com" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PolicyName cannot be null or empty*")
            .WithParameterName("PolicyName");
    }

    [Fact]
    public void Validate_WithNullPolicyName_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = null,
            AllowedOrigins = new[] { "https://example.com" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PolicyName cannot be null or empty*")
            .WithParameterName("PolicyName");
    }

    [Fact]
    public void Validate_WithWhitespacePolicyName_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "   ",
            AllowedOrigins = new[] { "https://example.com" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PolicyName cannot be null or empty*")
            .WithParameterName("PolicyName");
    }

    [Fact]
    public void Validate_WithNullAllowedOrigins_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = null
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowedOrigins cannot be null*")
            .WithParameterName("AllowedOrigins");
    }

    [Fact]
    public void Validate_WithEmptyAllowedOrigins_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = Array.Empty<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one allowed origin must be specified*")
            .WithParameterName("AllowedOrigins");
    }

    [Fact]
    public void Validate_WithNullOriginInArray_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new [] { "https://example.com", null }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowedOrigins cannot contain null or empty values*")
            .WithParameterName("AllowedOrigins");
    }

    [Fact]
    public void Validate_WithEmptyOriginInArray_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com", "" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowedOrigins cannot contain null or empty values*")
            .WithParameterName("AllowedOrigins");
    }

    [Fact]
    public void Validate_WithWildcardOrigin_ShouldNotThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "*" },
            AllowCredentials = false
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithInvalidOriginUrl_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "not-a-valid-url" }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid origin URL: 'not-a-valid-url'*")
            .WithParameterName("AllowedOrigins");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:3000")]
    [InlineData("https://sub.domain.example.com")]
    [InlineData("http://192.168.1.1:8080")]
    public void Validate_WithValidOriginUrls_ShouldNotThrow(string origin)
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { origin }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithWildcardAndAllowCredentials_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "*" },
            AllowCredentials = true
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowCredentials cannot be true when AllowedOrigins contains '*'*")
            .WithParameterName("AllowCredentials");
    }

    [Fact]
    public void Validate_WithExplicitOriginAndAllowCredentials_ShouldNotThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowCredentials = true
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullAllowedMethods_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedMethods = null
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one allowed HTTP method must be specified*")
            .WithParameterName("AllowedMethods");
    }

    [Fact]
    public void Validate_WithEmptyAllowedMethods_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedMethods = Array.Empty<string>()
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one allowed HTTP method must be specified*")
            .WithParameterName("AllowedMethods");
    }

    [Fact]
    public void Validate_WithNullMethodInArray_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedMethods = new[] { "GET", null }
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowedMethods cannot contain null or empty values*")
            .WithParameterName("AllowedMethods");
    }

    [Fact]
    public void Validate_WithNullAllowedHeaders_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedHeaders = null
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AllowedHeaders cannot be null*")
            .WithParameterName("AllowedHeaders");
    }

    [Fact]
    public void Validate_WithNullExposedHeaders_ShouldThrow()
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            ExposedHeaders = null
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ExposedHeaders cannot be null*")
            .WithParameterName("ExposedHeaders");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(86401)]
    public void Validate_WithInvalidPreflightMaxAge_ShouldThrow(int minutes)
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            PreflightMaxAgeMinutes = minutes
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PreflightMaxAgeMinutes must be between 1 and 86400*")
            .WithParameterName("PreflightMaxAgeMinutes");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(86400)]
    public void Validate_WithValidPreflightMaxAge_ShouldNotThrow(int minutes)
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            PreflightMaxAgeMinutes = minutes
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1000)]
    [InlineData(86401)]
    [InlineData(100000)]
    [InlineData(int.MaxValue)]
    public void Validate_WithBoundaryPreflightMaxAge_ShouldThrow(int minutes)
    {
        // Arrange
        var settings = new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            PreflightMaxAgeMinutes = minutes
        };

        // Act & Assert
        var act = () => settings.Validate();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PreflightMaxAgeMinutes must be between 1 and 86400*")
            .WithParameterName("PreflightMaxAgeMinutes");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new CorsSettings();

        // Assert
        settings.PolicyName.Should().Be("DefaultCorsPolicy");
        settings.AllowedOrigins.Should().BeEmpty();
        settings.AllowedMethods.Should().Equal("GET", "POST");
        settings.AllowedHeaders.Should().Equal(HeaderNames.ContentType, HeaderNames.Authorization, Headers.ApiKey);
        settings.ExposedHeaders.Should().Equal(
            HeaderNames.ContentType, 
            Headers.TraceId,
            Headers.RetryAfter,
            Headers.RateLimitLimit,
            Headers.RateLimitReset,
            Headers.RateLimitPolicy,
            Headers.RateLimitWindow);
        settings.AllowCredentials.Should().BeFalse();
        settings.PreflightMaxAgeMinutes.Should().Be(60);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var settings = new CorsSettings();

        // Act
        settings.PolicyName = "CustomPolicy";
        settings.AllowedOrigins = new[] { "https://custom.com" };
        settings.AllowedMethods = new[] { "GET", "POST", "PUT" };
        settings.AllowedHeaders = new[] { "Custom-Header" };
        settings.ExposedHeaders = new[] { "Custom-Response-Header" };
        settings.AllowCredentials = true;
        settings.PreflightMaxAgeMinutes = 120;

        // Assert
        settings.PolicyName.Should().Be("CustomPolicy");
        settings.AllowedOrigins.Should().Equal("https://custom.com");
        settings.AllowedMethods.Should().Equal("GET", "POST", "PUT");
        settings.AllowedHeaders.Should().Equal("Custom-Header");
        settings.ExposedHeaders.Should().Equal("Custom-Response-Header");
        settings.AllowCredentials.Should().BeTrue();
        settings.PreflightMaxAgeMinutes.Should().Be(120);
    }
}
