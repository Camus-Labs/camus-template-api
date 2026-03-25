using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class UserSettingsTests
{
    private const string ValidUsernameSecret = "user-secret";
    private const string ValidPasswordSecret = "pass-secret";
    private static readonly List<string> ValidAvailableRoles = new() { "admin", "reader" };

    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

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
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

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
            UsernameSecretName = new string('a', 51),
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

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
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

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
            PasswordSecretName = new string('a', 51),
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*must not exceed*50*characters*");
    }

    [Fact]
    public void Validate_NullRoles_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = null!
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one role*");
    }

    [Fact]
    public void Validate_EmptyRoles_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string>()
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one role*");
    }

    [Fact]
    public void Validate_InvalidRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string> { "nonexistent" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid roles*nonexistent*");
    }

    [Fact]
    public void Validate_NullAvailableRoles_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_MultipleValidRoles_DoesNotThrow()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = ValidUsernameSecret,
            PasswordSecretName = ValidPasswordSecret,
            Roles = new List<string> { "admin", "reader" }
        };

        // Act
        var act = () => settings.Validate(ValidAvailableRoles);

        // Assert
        act.Should().NotThrow();
    }
}
