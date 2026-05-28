using FluentAssertions;
using emc.camus.api.Configurations;
using emc.camus.application.RateLimiting;

namespace emc.camus.api.test.Configurations;

public class RateLimitingSettingsTests
{
    private const int DefaultPermitLimit = 100;
    private const int StrictPermitLimit = 10;
    private const int RelaxedPermitLimit = 500;
    private const int DefaultWindowSeconds = 60;
    private const string HealthPath = "/health";

    private static readonly string[] DefaultExemptPaths = new[] { HealthPath, "/ready" };

    private static readonly string[] ExemptPathsWithoutLeadingSlash = new[] { HealthPath, "metrics" };

    private static readonly string[] ExemptPathsWithNullEntry = new[] { HealthPath, null! };

    private static readonly string[] ExemptPathsWithEmptyEntry = new[] { HealthPath, "" };

    private static readonly string[] ExemptPathsWithWhitespaceEntry = new[] { HealthPath, "   " };

    private static readonly string[] EmptyExemptPaths = Array.Empty<string>();

    private static readonly Dictionary<string, RateLimitPolicySettings> EmptyPolicies = new();

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithoutDefault = new()
    {
        { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = RelaxedPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithNullValue = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Strict, null! }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithInvalidPermitLimit = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = 0, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = RelaxedPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithEmptyName = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { "", new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithWhitespaceName = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { "   ", new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> PoliciesWithUnknownName = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { "unknown-policy", new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static readonly Dictionary<string, RateLimitPolicySettings> DefaultPolicies = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = StrictPermitLimit, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = RelaxedPermitLimit, WindowSeconds = DefaultWindowSeconds } }
    };

    private static RateLimitingSettings CreateValidSettings(
        int segmentsPerWindow = 5,
        Dictionary<string, RateLimitPolicySettings>? policies = null,
        string[]? exemptPaths = null) =>
        new()
        {
            SegmentsPerWindow = segmentsPerWindow,
            Policies = policies ?? DefaultPolicies,
            ExemptPaths = exemptPaths ?? DefaultExemptPaths
        };

    // --- AC-06: Validate with correct section keys (Policies, ExemptPaths) ---

    [Fact]
    public void Validate_DefaultSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new RateLimitingSettings();

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
    [InlineData(21)]
    public void Validate_SegmentsPerWindowOutOfRange_ThrowsInvalidOperationException(int segments)
    {
        // Arrange
        var settings = CreateValidSettings(segmentsPerWindow: segments);

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
        var settings = CreateValidSettings(policies: EmptyPolicies);

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
        var settings = CreateValidSettings(policies: PoliciesWithoutDefault);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{RateLimitPolicies.Default}*");
    }

    public static IEnumerable<object[]> InvalidPolicyNameCases()
    {
        yield return new object[] { PoliciesWithEmptyName };
        yield return new object[] { PoliciesWithWhitespaceName };
    }

    [Theory]
    [MemberData(nameof(InvalidPolicyNameCases))]
    public void Validate_EmptyOrWhitespacePolicyName_ThrowsInvalidOperationException(
        object policiesObj)
    {
        // Arrange
        var policies = (Dictionary<string, RateLimitPolicySettings>)policiesObj;
        var settings = CreateValidSettings(policies: policies);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Policy name*null*empty*");
    }

    [Fact]
    public void Validate_UnknownPolicyName_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(policies: PoliciesWithUnknownName);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid policy name*unknown-policy*");
    }

    [Fact]
    public void Validate_NullPolicyValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(policies: PoliciesWithNullValue);

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
        var settings = CreateValidSettings(policies: PoliciesWithInvalidPermitLimit);

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
        var settings = CreateValidSettings(exemptPaths: EmptyExemptPaths);

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

    public static readonly TheoryData<string[]> InvalidExemptPathEntryCases = new()
    {
        { ExemptPathsWithNullEntry },
        { ExemptPathsWithEmptyEntry },
        { ExemptPathsWithWhitespaceEntry }
    };

    [Theory]
    [MemberData(nameof(InvalidExemptPathEntryCases))]
    public void Validate_ExemptPathWithInvalidEntry_ThrowsInvalidOperationException(string[] exemptPaths)
    {
        // Arrange
        var settings = CreateValidSettings(exemptPaths: exemptPaths);

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
            .WithMessage("*must start with*/*");
    }
}
