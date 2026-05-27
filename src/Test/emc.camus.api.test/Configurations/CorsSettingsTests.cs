using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class CorsSettingsTests
{
    private static readonly string[] ExpectedDefaultMethods = ["GET", "POST"];
    private static readonly string[] ValidOrigins = ["https://example.com"];
    private static readonly string[] WildcardOrigin = ["*"];
    private static readonly string[] OriginsWithEmpty = ["https://valid.com", ""];
    private static readonly string[] InvalidUrlOrigin = ["not-a-url"];
    private static readonly string[] ValidMethods = ["GET", "POST"];
    private static readonly string[] ValidHeaders = ["Content-Type"];
    private static readonly string[] ValidExposedHeaders = ["X-Custom"];

    private static CorsSettings CreateValidSettings()
    {
        return new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = ValidOrigins,
            AllowedMethods = ValidMethods,
            AllowedHeaders = ValidHeaders,
            ExposedHeaders = ValidExposedHeaders,
            PreflightMaxAgeMinutes = 60,
            AllowCredentials = false
        };
    }

    // --- Defaults ---

    [Fact]
    public void Constructor_Default_SetsExpectedDefaults()
    {
        // Act
        var settings = new CorsSettings();

        // Assert
        settings.PolicyName.Should().Be("DefaultCorsPolicy");
        settings.AllowedOrigins.Should().BeEmpty();
        settings.AllowedMethods.Should().BeEquivalentTo(ExpectedDefaultMethods);
        settings.AllowedHeaders.Should().HaveCount(3);
        settings.ExposedHeaders.Should().HaveCount(7);
        settings.AllowCredentials.Should().BeFalse();
        settings.PreflightMaxAgeMinutes.Should().Be(60);
    }

    // --- Validate: Valid Settings ---

    [Fact]
    public void Validate_ValidSettings_Succeeds()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WildcardOriginWithoutCredentials_Succeeds()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = WildcardOrigin;
        settings.AllowCredentials = false;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: PolicyName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidPolicyName_ThrowsInvalidOperationException(string? policyName)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.PolicyName = policyName!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PolicyName*null*empty*");
    }

    [Fact]
    public void Validate_PolicyNameExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.PolicyName = new string('a', 101);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PolicyName*must not exceed*100*");
    }

    // --- Validate: AllowedOrigins ---

    [Fact]
    public void Validate_NullAllowedOrigins_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedOrigins*null*");
    }

    [Fact]
    public void Validate_EmptyAllowedOrigins_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = Array.Empty<string>();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one*origin*");
    }

    [Fact]
    public void Validate_AllowedOriginsContainsEmptyValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = OriginsWithEmpty;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedOrigins*null*empty*");
    }

    [Fact]
    public void Validate_AllowedOriginsContainsInvalidUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = InvalidUrlOrigin;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid origin URL*");
    }

    // --- Validate: AllowCredentials ---

    [Fact]
    public void Validate_AllowCredentialsWithWildcardOrigin_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedOrigins = WildcardOrigin;
        settings.AllowCredentials = true;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowCredentials*cannot*true*AllowedOrigins*");
    }

    // --- Validate: AllowedMethods ---

    public static readonly TheoryData<string[]?> InvalidAllowedMethods = new()
    {
        null,
        Array.Empty<string>()
    };

    [Theory]
    [MemberData(nameof(InvalidAllowedMethods))]
    public void Validate_NullOrEmptyAllowedMethods_ThrowsInvalidOperationException(string[]? allowedMethods)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedMethods = allowedMethods!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one*HTTP method*");
    }

    public static readonly TheoryData<string[]> AllowedMethodsContainingNullOrWhitespace = new()
    {
        new[] { "GET", null! },
        new[] { "GET", "" },
        new[] { "GET", "   " }
    };

    [Theory]
    [MemberData(nameof(AllowedMethodsContainingNullOrWhitespace))]
    public void Validate_AllowedMethodsContainsNullOrWhitespace_ThrowsInvalidOperationException(string[] allowedMethods)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedMethods = allowedMethods;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedMethods*null or empty*");
    }

    // --- Validate: AllowedHeaders ---

    [Fact]
    public void Validate_NullAllowedHeaders_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AllowedHeaders = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedHeaders*null*");
    }

    // --- Validate: ExposedHeaders ---

    [Fact]
    public void Validate_NullExposedHeaders_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.ExposedHeaders = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExposedHeaders*null*");
    }

    // --- Validate: PreflightMaxAge ---

    [Theory]
    [InlineData(0)]
    [InlineData(86401)]
    public void Validate_PreflightMaxAgeOutOfRange_ThrowsInvalidOperationException(int maxAge)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.PreflightMaxAgeMinutes = maxAge;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PreflightMaxAgeMinutes*must be between*");
    }
}
