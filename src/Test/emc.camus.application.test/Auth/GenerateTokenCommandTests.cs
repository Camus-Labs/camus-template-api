using System.Linq;
using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class GenerateTokenCommandTests
{
    private const string ValidSuffix = "token1";
    private static readonly DateTime ValidExpiration = new(2099, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var suffix = ValidSuffix;
        var expiresOn = ValidExpiration;
        var permissions = new List<string> { Permissions.ApiRead };

        // Act
        var command = new GenerateTokenCommand(suffix, expiresOn, permissions);

        // Assert
        command.UsernameSuffix.Should().Be(suffix);
        command.ExpiresOn.Should().Be(expiresOn);
        command.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSuffix_ThrowsArgumentException(string? suffix)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenCommand(suffix!, ValidExpiration, [Permissions.ApiRead]);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("usernameSuffix");
    }

    [Fact]
    public void Constructor_DefaultExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultDate = default(DateTime);

        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, defaultDate, [Permissions.ApiRead]);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void Constructor_NullPermissions_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, ValidExpiration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("permissions");
    }

    [Fact]
    public void Constructor_EmptyPermissions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyPermissions = new List<string>();

        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, ValidExpiration, emptyPermissions);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("permissions.Count");
    }

    [Theory]
    [MemberData(nameof(InvalidPermissionsTestCases))]
    public void Constructor_InvalidPermissions_ThrowsArgumentException(
        List<string> permissions, string expectedInvalidPermission)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, ValidExpiration, permissions);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*Invalid permissions*{expectedInvalidPermission}*")
            .And.ParamName.Should().Be("permissions");
    }

    public static TheoryData<List<string>, string> InvalidPermissionsTestCases => new()
    {
        { new List<string> { "invalid.permission" }, "invalid.permission" },
        { new List<string> { Permissions.ApiRead, "nonexistent.perm" }, "nonexistent.perm" }
    };
}
