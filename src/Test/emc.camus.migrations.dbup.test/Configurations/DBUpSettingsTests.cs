using FluentAssertions;
using emc.camus.migrations.dbup.Configurations;

namespace emc.camus.migrations.dbup.test.Configurations;

public class DBUpSettingsTests
{
    private const string ValidAdminSecretName = "db-admin-username";
    private const string ValidPasswordSecretName = "db-admin-password";

    // --- Defaults ---

    [Fact]
    public void Constructor_Defaults_SetsExpectedValues()
    {
        // Arrange
        // Act
        var settings = new DBUpSettings();

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.AdminSecretName.Should().BeEmpty();
        settings.PasswordSecretName.Should().BeEmpty();
    }

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

    [Fact]
    public void Validate_DisabledWithEmptySecrets_DoesNotThrow()
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = false,
            AdminSecretName = string.Empty,
            PasswordSecretName = string.Empty
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate when Enabled - Valid ---

    [Fact]
    public void Validate_EnabledWithValidSecrets_DoesNotThrow()
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = ValidAdminSecretName,
            PasswordSecretName = ValidPasswordSecretName
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EnabledWithMaxLengthSecretNames_DoesNotThrow()
    {
        // Arrange
        var settings = new DBUpSettings
        {
            Enabled = true,
            AdminSecretName = new string('a', 200),
            PasswordSecretName = new string('b', 200)
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
            PasswordSecretName = ValidPasswordSecretName
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
            PasswordSecretName = ValidPasswordSecretName
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
            AdminSecretName = ValidAdminSecretName,
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
            AdminSecretName = ValidAdminSecretName,
            PasswordSecretName = new string('p', 201)
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*exceed*200*");
    }

    // --- ConfigurationSectionName ---

    [Fact]
    public void ConfigurationSectionName_ReturnsExpectedValue()
    {
        // Arrange
        // Act
        // Assert
        DBUpSettings.ConfigurationSectionName.Should().Be("DBUpSettings");
    }
}
