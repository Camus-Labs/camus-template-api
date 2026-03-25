using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class RoleSettingsTests
{
    private const string ValidRoleName = "admin";

    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = new List<string> { Permissions.ApiRead }
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
            Permissions = new List<string> { Permissions.ApiRead }
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
            Permissions = new List<string> { Permissions.ApiRead }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Name*must not exceed*50*characters*");
    }

    [Fact]
    public void Validate_NullPermissions_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = null!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one permission*");
    }

    [Fact]
    public void Validate_EmptyPermissions_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = new List<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one permission*");
    }

    [Fact]
    public void Validate_InvalidPermission_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = new List<string> { "invalid.permission" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid permissions*invalid.permission*");
    }

    [Fact]
    public void Validate_MixedValidAndInvalidPermissions_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = new List<string> { Permissions.ApiRead, "nonexistent.perm" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid permissions*nonexistent.perm*");
    }

    [Fact]
    public void Validate_AllValidPermissions_DoesNotThrow()
    {
        // Arrange
        var settings = new RoleSettings
        {
            Name = ValidRoleName,
            Permissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite, Permissions.TokenCreate }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
