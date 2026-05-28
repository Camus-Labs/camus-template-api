using FluentAssertions;
using emc.camus.application.ApiInfo;

namespace emc.camus.application.test.ApiInfo;

public class ApiInfoFilterTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_ValidVersion_SetsProperty()
    {
        // Arrange
        var version = "1.0";

        // Act
        var filter = new ApiInfoFilter(version);

        // Assert
        filter.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidVersion_ThrowsArgumentException(string? version)
    {
        // Arrange
        // Act
        var act = () => new ApiInfoFilter(version!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("version");
    }

    [Fact]
    public void Constructor_VersionExceedsMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var longVersion = new string('a', 51);

        // Act
        var act = () => new ApiInfoFilter(longVersion);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("Version");
    }
}
