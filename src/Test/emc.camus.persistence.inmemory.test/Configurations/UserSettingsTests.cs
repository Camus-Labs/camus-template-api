using FluentAssertions;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class UserSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = new UserSettings
        {
            UsernameSecretName = "user-secret",
            PasswordSecretName = "pass-secret",
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            PasswordSecretName = "pass-secret",
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            PasswordSecretName = "pass-secret",
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            UsernameSecretName = "user-secret",
            PasswordSecretName = passwordSecret!,
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            UsernameSecretName = "user-secret",
            PasswordSecretName = new string('a', 51),
            Roles = new List<string> { "admin" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            UsernameSecretName = "user-secret",
            PasswordSecretName = "pass-secret",
            Roles = roles!
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            UsernameSecretName = "user-secret",
            PasswordSecretName = "pass-secret",
            Roles = new List<string> { "nonexistent" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

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
            UsernameSecretName = "user-secret",
            PasswordSecretName = "pass-secret",
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
            UsernameSecretName = "user-secret",
            PasswordSecretName = "pass-secret",
            Roles = new List<string> { "admin", "reader" }
        };

        // Act
        var act = () => settings.Validate(new List<string> { "admin", "reader" });

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object?[]> NullOrEmptyRolesData() => new List<object?[]>
    {
        new object?[] { null },
        new object?[] { new List<string>() }
    };
}
