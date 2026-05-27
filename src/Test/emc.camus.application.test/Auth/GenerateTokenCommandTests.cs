using System.Linq;
using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class GenerateTokenCommandTests
{
    private const string ValidSuffix = "token1";
    private static readonly DateTimeOffset ReferenceTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiration = ReferenceTime.UtcDateTime.AddMonths(6);
    private static readonly IReadOnlyList<string> ValidPermissions = [Permissions.ApiRead];
    private static readonly IReadOnlyList<string> EmptyPermissions = [];
    private static readonly List<string> InvalidPermissionOnly = ["invalid.permission"];
    private static readonly List<string> MixedWithInvalidPermission = [Permissions.ApiRead, "nonexistent.perm"];

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var suffix = ValidSuffix;
        var expiresOn = ValidExpiration;
        var permissions = ValidPermissions.ToList();

        // Act
        var command = new GenerateTokenCommand(suffix, expiresOn, permissions);

        // Assert
        command.UsernameSuffix.Should().Be(suffix);
        command.ExpiresOn.Should().Be(expiresOn);
        command.Permissions.Should().BeEquivalentTo(ValidPermissions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSuffix_ThrowsArgumentException(string? suffix)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenCommand(suffix!, ValidExpiration, ValidPermissions.ToList());

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("UsernameSuffix");
    }

    [Fact]
    public void Constructor_SuffixExceedsMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var longSuffix = new string('a', 21);

        // Act
        var act = () => new GenerateTokenCommand(longSuffix, ValidExpiration, ValidPermissions.ToList());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("UsernameSuffix");
    }

    [Fact]
    public void Constructor_DefaultExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultDate = default(DateTime);

        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, defaultDate, ValidPermissions.ToList());

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
        // Act
        var act = () => new GenerateTokenCommand(ValidSuffix, ValidExpiration, EmptyPermissions.ToList());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("Permissions");
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
            .And.ParamName.Should().Be("Permissions");
    }

    public static readonly TheoryData<List<string>, string> InvalidPermissionsTestCases = new()
    {
        { InvalidPermissionOnly, "invalid.permission" },
        { MixedWithInvalidPermission, "nonexistent.perm" }
    };
}
