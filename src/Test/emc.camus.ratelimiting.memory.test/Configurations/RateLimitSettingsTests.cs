using emc.camus.application.RateLimiting;
using emc.camus.ratelimiting.memory.Configurations;
using FluentAssertions;

namespace emc.camus.ratelimiting.memory.test.Configurations;

public class RateLimitSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = new[] { "/health", "/ready" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public void Validate_WithInvalidSegmentsPerWindow_ThrowsArgumentException(int segments)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = segments,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("SegmentsPerWindow must be between 1 and 20*")
            .And.ParamName.Should().Be("SegmentsPerWindow");
    }

    [Theory]
    [InlineData(1)]      // Minimum valid value
    [InlineData(5)]      // Common default
    [InlineData(10)]     // Higher granularity
    [InlineData(20)]     // Maximum valid value
    public void Validate_WithValidSegmentsPerWindow_DoesNotThrow(int segments)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = segments,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullPolicies_ThrowsArgumentException()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = null
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one rate limit policy must be defined*")
            .And.ParamName.Should().Be("Policies");
    }

    [Fact]
    public void Validate_WithEmptyPolicies_ThrowsArgumentException()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one rate limit policy must be defined*")
            .And.ParamName.Should().Be("Policies");
    }

    [Fact]
    public void Validate_WithoutDefaultPolicy_ThrowsArgumentException()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { "custom", new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"A '{RateLimitPolicies.Default}' rate limit policy must be defined*")
            .And.ParamName.Should().Be("Policies");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPolicyName_ThrowsArgumentException(string policyName)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } },
                { policyName, new RateLimitPolicy { PermitLimit = 50, WindowSeconds = 60 } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Policy name cannot be null or empty*")
            .And.ParamName.Should().Be("Policies");
    }

    [Fact]
    public void Validate_WithNullPolicy_ThrowsArgumentException()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } },
                { "custom", null }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Policy 'custom' cannot be null*")
            .And.ParamName.Should().Be("Policies");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public void Validate_WithInvalidPolicyPermitLimit_ThrowsArgumentException(int permitLimit)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = permitLimit, WindowSeconds = 60 } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Policy '{RateLimitPolicies.Default}': PermitLimit must be between 1 and 100,000*")
            .And.ParamName.Should().Be("PermitLimit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void Validate_WithInvalidPolicyWindowSeconds_ThrowsArgumentException(int windowSeconds)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = windowSeconds } }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Policy '{RateLimitPolicies.Default}': WindowSeconds must be between 1 and 3,600*")
            .And.ParamName.Should().Be("WindowSeconds");
    }

    [Fact]
    public void Validate_WithNullExemptPaths_ThrowsArgumentException()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = null
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ExemptPaths cannot be null*")
            .And.ParamName.Should().Be("ExemptPaths");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyExemptPath_ThrowsArgumentException(string? exemptPath)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = new[] { "/health", exemptPath! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ExemptPaths cannot contain null or empty values*")
            .And.ParamName.Should().Be("ExemptPaths");
    }

    [Theory]
    [InlineData("health")]
    [InlineData("api/health")]
    [InlineData("ready")]
    public void Validate_WithExemptPathNotStartingWithSlash_ThrowsArgumentException(string exemptPath)
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = new[] { exemptPath }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"ExemptPath '{exemptPath}' must start with '/'*")
            .And.ParamName.Should().Be("ExemptPaths");
    }

    [Fact]
    public void Validate_WithEmptyExemptPaths_DoesNotThrow()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = Array.Empty<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("/api/[a-z]+")]
    [InlineData("/users/*")]
    [InlineData("/items/{id}")]
    public void Validate_WithRegexOrWildcardPatterns_TreatedAsLiteralPaths(string path)
    {
        // Arrange
        // Note: ExemptPaths uses literal string matching (StartsWith), not regex patterns.
        // Regex-like or wildcard characters are treated as literal characters.
        var settings = new RateLimitSettings
        {
            SegmentsPerWindow = 5,
            Policies = new Dictionary<string, RateLimitPolicy>
            {
                { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 100, WindowSeconds = 60 } }
            },
            ExemptPaths = new[] { path }
        };

        // Act & Assert
        // These paths are valid because they start with '/' - no regex validation is performed
        var act = () => settings.Validate();
        act.Should().NotThrow();
    }
}
