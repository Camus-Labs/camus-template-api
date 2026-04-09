using FluentAssertions;
using emc.camus.ratelimiting.inmemory.Configurations;
using emc.camus.application.RateLimiting;

namespace emc.camus.ratelimiting.inmemory.test.Configurations;

public class InMemoryRateLimitingSettingsTests
{
    private static readonly string[] DefaultExemptPaths = new[] { "/health", "/ready" };
    private static readonly string[] ExemptPathsWithoutLeadingSlash = new[] { "/health", "metrics" };

    private static InMemoryRateLimitingSettings CreateValidSettings(
        int segmentsPerWindow = 5,
        Dictionary<string, RateLimitPolicySettings>? policies = null,
        string[]? exemptPaths = null) =>
        new()
        {
            SegmentsPerWindow = segmentsPerWindow,
            Policies = policies ?? new Dictionary<string, RateLimitPolicySettings>
            {
                { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = 100, WindowSeconds = 60 } },
                { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = 10, WindowSeconds = 60 } },
                { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = 500, WindowSeconds = 60 } }
            },
            ExemptPaths = exemptPaths ?? DefaultExemptPaths
        };

    // --- Validate: Valid Configuration ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new InMemoryRateLimitingSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: SegmentsPerWindow ---

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public void Validate_ValidSegmentsPerWindow_DoesNotThrow(int segments)
    {
        // Arrange
        var settings = CreateValidSettings(segmentsPerWindow: segments);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_SegmentsPerWindowBelowMinimum_ThrowsInvalidOperationException(int segments)
    {
        // Arrange
        var settings = CreateValidSettings(segmentsPerWindow: segments);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SegmentsPerWindow*");
    }

    [Fact]
    public void Validate_SegmentsPerWindowAboveMaximum_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(segmentsPerWindow: 21);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SegmentsPerWindow*");
    }

    // --- Validate: Policies ---

    [Fact]
    public void Validate_NullPolicies_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Policies = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*policy*");
    }

    [Fact]
    public void Validate_EmptyPolicies_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(policies: new Dictionary<string, RateLimitPolicySettings>());

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*policy*");
    }

    [Fact]
    public void Validate_MissingDefaultPolicy_ThrowsInvalidOperationException()
    {
        // Arrange
        var policies = new Dictionary<string, RateLimitPolicySettings>
        {
            { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = 10, WindowSeconds = 60 } },
            { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = 500, WindowSeconds = 60 } }
        };
        var settings = CreateValidSettings(policies: policies);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{RateLimitPolicies.Default}*");
    }

    [Theory]
    [MemberData(nameof(InvalidPolicyNameCases))]
    public void Validate_InvalidPolicyName_ThrowsInvalidOperationException(string policyName, string expectedMessagePattern)
    {
        // Arrange
        var policies = new Dictionary<string, RateLimitPolicySettings>
        {
            { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = 100, WindowSeconds = 60 } },
            { policyName, new RateLimitPolicySettings { PermitLimit = 10, WindowSeconds = 60 } }
        };
        var settings = CreateValidSettings(policies: policies);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessagePattern);
    }

    public static TheoryData<string, string> InvalidPolicyNameCases => new()
    {
        { "", "*Policy name*null*empty*" },
        { "   ", "*Policy name*null*empty*" },
        { "unknown-policy", "*Invalid policy name*unknown-policy*" }
    };

    [Fact]
    public void Validate_NullPolicyValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var policies = new Dictionary<string, RateLimitPolicySettings>
        {
            { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = 100, WindowSeconds = 60 } },
            { RateLimitPolicies.Strict, null! }
        };
        var settings = CreateValidSettings(policies: policies);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be null*");
    }

    [Fact]
    public void Validate_PolicyWithInvalidPermitLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        var policies = new Dictionary<string, RateLimitPolicySettings>
        {
            { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = 0, WindowSeconds = 60 } },
            { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = 10, WindowSeconds = 60 } },
            { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = 500, WindowSeconds = 60 } }
        };
        var settings = CreateValidSettings(policies: policies);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PermitLimit*");
    }

    // --- Validate: ExemptPaths ---

    [Fact]
    public void Validate_EmptyExemptPaths_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings(exemptPaths: Array.Empty<string>());

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullExemptPaths_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.ExemptPaths = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExemptPaths*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ExemptPathNullOrEmpty_ThrowsInvalidOperationException(string? invalidPath)
    {
        // Arrange
        var settings = CreateValidSettings(exemptPaths: new[] { "/health", invalidPath! });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExemptPaths*null*empty*");
    }

    [Fact]
    public void Validate_ExemptPathWithoutLeadingSlash_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(exemptPaths: ExemptPathsWithoutLeadingSlash);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*metrics*start with*/*");
    }
}
