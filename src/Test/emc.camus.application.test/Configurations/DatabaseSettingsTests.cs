using FluentAssertions;
using emc.camus.application.Configurations;

namespace emc.camus.application.test.Configurations;

public class DatabaseSettingsTests
{
    private static DatabaseSettings CreateValidSettings()
    {
        return new DatabaseSettings
        {
            Host = "localhost",
            Port = 5432,
            Database = "testdb",
            UserSecretName = "db-user",
            PasswordSecretName = "db-password"
        };
    }

    // --- Validate ---

    [Fact]
    public void Validate_DefaultSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new DatabaseSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Host*");
    }

    [Fact]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- ValidateHost ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidHost_ThrowsInvalidOperationException(string? host)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Host = host!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Host*");
    }

    // --- ValidatePort ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_InvalidPort_ThrowsInvalidOperationException(int port)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Port = port;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Port*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5432)]
    [InlineData(65535)]
    public void Validate_ValidPort_DoesNotThrow(int port)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Port = port;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- ValidateDatabase ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidDatabase_ThrowsInvalidOperationException(string? database)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Database = database!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Database*");
    }

    // --- ValidateUserSecretName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidUserSecretName_ThrowsInvalidOperationException(string? secretName)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.UserSecretName = secretName!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UserSecretName*");
    }

    [Fact]
    public void Validate_UserSecretNameTooLong_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.UserSecretName = new string('a', 51);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UserSecretName*exceed*");
    }

    // --- ValidatePasswordSecretName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidPasswordSecretName_ThrowsInvalidOperationException(string? secretName)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.PasswordSecretName = secretName!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*");
    }

    [Fact]
    public void Validate_PasswordSecretNameTooLong_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.PasswordSecretName = new string('a', 51);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PasswordSecretName*exceed*");
    }

    // --- ValidateAdditionalParameters ---

    [Fact]
    public void Validate_NullAdditionalParameters_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AdditionalParameters = null;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidAdditionalParameters_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AdditionalParameters = "SslMode=Require;Pooling=true";

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AdditionalParametersTooLong_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AdditionalParameters = new string('a', 101);

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AdditionalParameters*exceed*");
    }
}
