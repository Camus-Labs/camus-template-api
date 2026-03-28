using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class CorsSettingsTests
{
    private static CorsSettings CreateValidSettings()
    {
        return new CorsSettings
        {
            PolicyName = "TestPolicy",
            AllowedOrigins = new[] { "https://example.com" },
            AllowedMethods = new[] { "GET", "POST" },
            AllowedHeaders = new[] { "Content-Type" },
            ExposedHeaders = new[] { "X-Custom" },
            PreflightMaxAgeMinutes = 60,
            AllowCredentials = false
        };
    }

    // --- Defaults ---

    [Fact]
    public void Constructor_Default_SetsExpectedDefaults()
    {
        // Arrange
        string[] expectedMethods = ["GET", "POST"];

        // Act
        var settings = new CorsSettings();

        // Assert
        settings.PolicyName.Should().Be("DefaultCorsPolicy");
        settings.AllowedOrigins.Should().BeEmpty();
        settings.AllowedMethods.Should().BeEquivalentTo(expectedMethods);
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
        settings.AllowedOrigins = new[] { "*" };
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
        settings.AllowedOrigins = new[] { "https://valid.com", "" };

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
        settings.AllowedOrigins = new[] { "not-a-url" };

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
        settings.AllowedOrigins = new[] { "*" };
        settings.AllowCredentials = true;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowCredentials*cannot*true*AllowedOrigins*");
    }

    // --- Validate: AllowedMethods ---

    public static IEnumerable<object?[]> InvalidAllowedMethods()
    {
        yield return new object?[] { null };
        yield return new object?[] { Array.Empty<string>() };
    }

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
