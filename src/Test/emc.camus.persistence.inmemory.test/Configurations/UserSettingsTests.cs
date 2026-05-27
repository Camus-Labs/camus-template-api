using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class UserSettingsTests
{
    private const string ValidUsernameSecret = "user-secret";
    private const string ValidPasswordSecret = "pass-secret";
    private const string ValidRoleName = "admin";
    private static readonly string OverMaxLengthValue = new string('a', 51);
    private static readonly List<string> DefaultRolesArray = [ValidRoleName];
    private static readonly List<string> MultipleRolesArray = [ValidRoleName, "reader"];

    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = DefaultRolesArray
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
    public void Validate_InvalidUsernameSecretName_ThrowsInvalidOperationException(string? usernameSecret)
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = usernameSecret!,
            PasswordSecretName = ValidPasswordSecret,
            Roles = DefaultRolesArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UsernameSecretName*cannot be null or empty*");
    }

    [Fact]
    public void Validate_UsernameSecretNameExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = OverMaxLengthValue,
            PasswordSecretName = ValidPasswordSecret,
            Roles = DefaultRolesArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UsernameSecretName*must not exceed*50*characters*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidPasswordSecretName_ThrowsInvalidOperationException(string? passwordSecret)
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = passwordSecret!,
            Roles = DefaultRolesArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have a PasswordSecretName*");
    }

    [Fact]
    public void Validate_PasswordSecretNameExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = OverMaxLengthValue,
            Roles = DefaultRolesArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*must not exceed*50*characters*");
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyRolesData))]
    public void Validate_NullOrEmptyRoles_ThrowsInvalidOperationException(List<string>? roles)
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = roles!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one role*");
    }

    [Fact]
    public void Validate_MultipleRoles_DoesNotThrow()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = MultipleRolesArray
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static readonly TheoryData<List<string>?> NullOrEmptyRolesData = new()
    {
        { null },
        { new List<string>() }
    };
}
