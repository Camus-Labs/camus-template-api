using FluentAssertions;
using emc.camus.migrations.dbup.Configurations;

namespace emc.camus.migrations.dbup.test.Configurations;

public class DBUpSettingsTests
{
    private const string AdminSecretValue = "db-admin-username";
    private const string PasswordSecretValue = "db-admin-password";
    private const int MaxSecretNameLength = 200;

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

    public static readonly TheoryData<string, string> ValidSecretNameData = new()
    {
        { AdminSecretValue, PasswordSecretValue },
        { new string('a', MaxSecretNameLength), new string('b', MaxSecretNameLength) }
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
            PasswordSecretName = PasswordSecretValue
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdminSecretName*null*empty*");
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
            AdminSecretName = AdminSecretValue,
            PasswordSecretName = passwordSecretName!
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*null*empty*");
    }

    // --- Validate SecretName MaxLength ---

    public static readonly TheoryData<string, string, string> SecretNameExceedingMaxLengthData = new()
    {
        { new string('a', MaxSecretNameLength + 1), PasswordSecretValue, $"*AdminSecretName*exceed*{MaxSecretNameLength}*" },
        { AdminSecretValue, new string('p', MaxSecretNameLength + 1), $"*PasswordSecretName*exceed*{MaxSecretNameLength}*" }
    };

    [Theory]
    [MemberData(nameof(SecretNameExceedingMaxLengthData))]
    public void Validate_EnabledWithSecretNameExceedingMaxLength_ThrowsInvalidOperationException(
        string adminSecretName, string passwordSecretName, string expectedMessage)
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }
}
