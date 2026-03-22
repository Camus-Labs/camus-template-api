using FluentAssertions;
using emc.camus.documentation.swagger.Configurations;

namespace emc.camus.documentation.swagger.test.Configurations;

public class ApiVersionSettingsTests
{
    private const string ValidVersion = "v1";
    private const string ValidTitle = "My API";
    private const string ValidDescription = "API description";

    // --- Validate (valid settings) ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Version validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidVersion_ThrowsInvalidOperationException(string? version)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Version = version!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Version*null*empty*");
    }

    // --- Title validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidTitle_ThrowsInvalidOperationException(string? title)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Title = title!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Title*null*empty*");
    }

    // --- Description validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidDescription_ThrowsInvalidOperationException(string? description)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Description = description!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Description*null*empty*");
    }


    private static ApiVersionSettings CreateValidSettings()
    {
        return new ApiVersionSettings
        {
            Version = ValidVersion,
            Title = ValidTitle,
            Description = ValidDescription
        };
    }
}
