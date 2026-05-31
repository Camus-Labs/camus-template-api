using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class SwaggerSettingsTests
{
    private const string InvalidScheme = "InvalidScheme";

    private static readonly List<ApiVersionSettings> ValidVersionsList =
    [
        new() { Version = "v1", Title = "Test API", Description = "Test API Description" }
    ];

    private static readonly List<ApiVersionSettings> EmptyVersionsList = [];

    private static readonly List<ApiVersionSettings> NullEntryVersionsList = [null!];

    private static readonly List<ApiVersionSettings> InvalidEntryVersionsList =
    [
        new() { Version = "", Title = "Test", Description = "Desc" }
    ];

    private static readonly List<string> EmptySchemesList = [];

    private static readonly List<string> JwtBearerSchemesList = ["Bearer"];

    private static readonly List<string> AllSchemesList = ["Bearer", "ApiKey"];

    private static readonly List<string> InvalidSchemesList = [InvalidScheme];

    // --- AC-01: Solution builds with SwaggerSettings in API namespace ---
    // --- AC-03: Validates security schemes are configured correctly ---

    [Theory]
    [MemberData(nameof(ValidSettingsData))]
    public void Validate_ValidSettings_DoesNotThrow(SwaggerSettings settings)
    {
        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object[]> ValidSettingsData()
    {
        yield return [new SwaggerSettings { Enabled = false }];
        yield return [new SwaggerSettings
        {
            Enabled = true,
            Versions = ValidVersionsList,
            SecuritySchemes = JwtBearerSchemesList
        }];
        yield return [new SwaggerSettings
        {
            Enabled = true,
            Versions = ValidVersionsList,
            SecuritySchemes = EmptySchemesList
        }];
        yield return [new SwaggerSettings
        {
            Enabled = true,
            Versions = ValidVersionsList,
            SecuritySchemes = AllSchemesList
        }];
        yield return [new SwaggerSettings
        {
            Enabled = false,
            SecuritySchemes = InvalidSchemesList
        }];
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
            Versions = EmptyVersionsList
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
            Versions = NullEntryVersionsList
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
            Versions = InvalidEntryVersionsList
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Version*null*empty*");
    }

    // --- SecuritySchemes validation (AC-03: security scheme definitions preserved) ---

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
        settings.SecuritySchemes = InvalidSchemesList;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Invalid security scheme*{InvalidScheme}*");
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

    private static SwaggerSettings CreateValidEnabledSettings()
    {
        return new SwaggerSettings
        {
            Enabled = true,
            Versions = ValidVersionsList,
            SecuritySchemes = JwtBearerSchemesList
        };
    }
}
