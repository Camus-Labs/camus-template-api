using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class RateLimitingSettingsTests
{
    private const int DefaultPermitLimit = 250;
    private const int DefaultWindowSeconds = 60;
    private const int DefaultStrictPermitLimit = 50;
    private const int DefaultStrictWindowSeconds = 60;
    private const int DefaultRelaxedPermitLimit = 500;
    private const int DefaultRelaxedWindowSeconds = 60;
    private const string HealthPath = "/health";

    private static readonly string[] DefaultExemptPaths = new[] { HealthPath, "/ready" };

    private static readonly string[] ExemptPathsWithoutLeadingSlash = new[] { HealthPath, "metrics" };

    private static readonly string[] ExemptPathsWithNullEntry = new[] { HealthPath, null! };

    private static readonly string[] ExemptPathsWithEmptyEntry = new[] { HealthPath, "" };

    private static readonly string[] ExemptPathsWithWhitespaceEntry = new[] { HealthPath, "   " };

    private static readonly string[] EmptyExemptPaths = Array.Empty<string>();

    private static RateLimitingSettings CreateValidSettings(
        int segmentsPerWindow = 5,
        int defaultPermitLimit = 100,
        int defaultWindowSeconds = 60,
        int strictPermitLimit = 10,
        int strictWindowSeconds = 60,
        int relaxedPermitLimit = 500,
        int relaxedWindowSeconds = 60,
        string[]? exemptPaths = null) =>
        new()
        {
            SegmentsPerWindow = segmentsPerWindow,
            DefaultPermitLimit = defaultPermitLimit,
            DefaultWindowSeconds = defaultWindowSeconds,
            StrictPermitLimit = strictPermitLimit,
            StrictWindowSeconds = strictWindowSeconds,
            RelaxedPermitLimit = relaxedPermitLimit,
            RelaxedWindowSeconds = relaxedWindowSeconds,
            ExemptPaths = exemptPaths ?? DefaultExemptPaths
        };

    // --- AC-01: RateLimitingSettings has no Policies dictionary property ---

    [Fact]
    public void RateLimitingSettings_DoesNotHavePoliciesDictionaryProperty()
    {
        // Act
        var properties = typeof(RateLimitingSettings).GetProperties();

        // Assert
        properties.Should().NotContain(p => p.Name == "Policies");
    }

    // --- AC-02: Flat keys exist with correct default values ---

    [Fact]
    public void DefaultSettings_HasExpectedFlatPropertyDefaults()
    {
        // Arrange
        var settings = new RateLimitingSettings();

        // Assert
        settings.DefaultPermitLimit.Should().Be(DefaultPermitLimit);
        settings.DefaultWindowSeconds.Should().Be(DefaultWindowSeconds);
        settings.StrictPermitLimit.Should().Be(DefaultStrictPermitLimit);
        settings.StrictWindowSeconds.Should().Be(DefaultStrictWindowSeconds);
        settings.RelaxedPermitLimit.Should().Be(DefaultRelaxedPermitLimit);
        settings.RelaxedWindowSeconds.Should().Be(DefaultRelaxedWindowSeconds);
    }

    // --- Validate: SegmentsPerWindow ---

    [Fact]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

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

    // --- Validate: Permit Limits ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_DefaultPermitLimitInvalid_ThrowsInvalidOperationException(int permitLimit)
    {
        // Arrange
        var settings = CreateValidSettings(defaultPermitLimit: permitLimit);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultPermitLimit*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_StrictPermitLimitInvalid_ThrowsInvalidOperationException(int permitLimit)
    {
        // Arrange
        var settings = CreateValidSettings(strictPermitLimit: permitLimit);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StrictPermitLimit*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RelaxedPermitLimitInvalid_ThrowsInvalidOperationException(int permitLimit)
    {
        // Arrange
        var settings = CreateValidSettings(relaxedPermitLimit: permitLimit);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RelaxedPermitLimit*");
    }

    // --- Validate: Window Seconds ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_DefaultWindowSecondsInvalid_ThrowsInvalidOperationException(int windowSeconds)
    {
        // Arrange
        var settings = CreateValidSettings(defaultWindowSeconds: windowSeconds);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultWindowSeconds*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_StrictWindowSecondsInvalid_ThrowsInvalidOperationException(int windowSeconds)
    {
        // Arrange
        var settings = CreateValidSettings(strictWindowSeconds: windowSeconds);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StrictWindowSeconds*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RelaxedWindowSecondsInvalid_ThrowsInvalidOperationException(int windowSeconds)
    {
        // Arrange
        var settings = CreateValidSettings(relaxedWindowSeconds: windowSeconds);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RelaxedWindowSeconds*");
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
