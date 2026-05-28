using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class RoleSettingsTests
{
    private const string ValidRoleName = "admin";
    private static readonly List<string> ReadPermissionArray = [Permissions.ApiRead];
    private static readonly List<string> AllValidPermissionsArray = [Permissions.ApiRead, Permissions.ApiWrite, Permissions.TokenCreate];

    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = ReadPermissionArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidName_ThrowsInvalidOperationException(string? name)
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = name!,
            Permissions = ReadPermissionArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Name*cannot be null or empty*");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = new string('a', 51),
            Permissions = ReadPermissionArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Name*must not exceed*50*characters*");
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyPermissionsData))]
    public void Validate_NullOrEmptyPermissions_ThrowsInvalidOperationException(List<string>? permissions)
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = permissions!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one permission*");
    }

    [Theory]
    [MemberData(nameof(InvalidPermissionsData))]
    public void Validate_InvalidPermissions_ThrowsInvalidOperationException(
        List<string> permissions, string expectedInvalidPermission)
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = permissions
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*invalid permissions*{expectedInvalidPermission}*");
    }

    [Fact]
    public void Validate_AllValidPermissions_DoesNotThrow()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = AllValidPermissionsArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static readonly TheoryData<List<string>?> NullOrEmptyPermissionsData = new()
    {
        { null },
        { new List<string>() }
    };

    public static readonly TheoryData<List<string>, string> InvalidPermissionsData = new()
    {
        { new List<string> { "invalid.permission" }, "invalid.permission" },
        { new List<string> { Permissions.ApiRead, "nonexistent.perm" }, "nonexistent.perm" }
    };
}
