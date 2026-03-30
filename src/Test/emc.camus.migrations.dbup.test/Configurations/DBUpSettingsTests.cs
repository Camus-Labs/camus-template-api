using FluentAssertions;
using emc.camus.migrations.dbup.Configurations;

namespace emc.camus.migrations.dbup.test.Configurations;

public class DBUpSettingsTests
{
    // --- Validate when Disabled ---

    [Fact]
    public void Validate_DisabledWithNoSecrets_DoesNotThrow()
    {
        // Arrange
        var settings = new DBUpSettings { Enabled = false };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate when Enabled - Valid ---

    public static TheoryData<string, string> ValidSecretNameData => new()
    {
        { "db-admin-username", "db-admin-password" },
        { new string('a', 200), new string('b', 200) }
    };

    [Theory]
    [MemberData(nameof(ValidSecretNameData))]
    public void Validate_EnabledWithValidSecrets_DoesNotThrow(string adminSecretName, string passwordSecretName)
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = adminSecretName,
            PasswordSecretName = passwordSecretName
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate AdminSecretName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EnabledWithInvalidAdminSecretName_ThrowsInvalidOperationException(string? adminSecretName)
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = adminSecretName!,
            PasswordSecretName = "db-admin-password"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdminSecretName*null*empty*");
    }

    [Fact]
    public void Validate_EnabledWithAdminSecretNameExceedingMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = new string('a', 201),
            PasswordSecretName = "db-admin-password"
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdminSecretName*exceed*200*");
    }

    // --- Validate PasswordSecretName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EnabledWithInvalidPasswordSecretName_ThrowsInvalidOperationException(string? passwordSecretName)
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = "db-admin-username",
            PasswordSecretName = passwordSecretName!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*null*empty*");
    }

    [Fact]
    public void Validate_EnabledWithPasswordSecretNameExceedingMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = "db-admin-username",
            PasswordSecretName = new string('p', 201)
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*exceed*200*");
    }
}
