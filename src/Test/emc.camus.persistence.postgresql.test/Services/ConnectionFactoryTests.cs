using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.application.Secrets;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class ConnectionFactoryTests
{
    private const string UserSecretName = "db-user";
    private const string PasswordSecretName = "db-password";
    private const string ValidUser = "validuser";
    private const string ValidPass = "validpass";

    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ISecretProvider> _mockSecretProvider;

    public ConnectionFactoryTests()
    {
        _mockUserContext = new Mock<IUserContext>();
        _mockSecretProvider = new Mock<ISecretProvider>();
    }

    private static DatabaseSettings CreateValidSettings() => new()
    {
        Host = "localhost",
        Port = 5432,
        Database = "testdb",
        UserSecretName = UserSecretName,
        PasswordSecretName = PasswordSecretName
    };

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ConnectionFactory(null!, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("settings");
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => new ConnectionFactory(settings, null!, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userContext");
    }

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => new ConnectionFactory(settings, _mockUserContext.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("secretProvider");
    }

    [Fact]
    public void Constructor_EmptyUsernameSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret(UserSecretName)).Returns(string.Empty);

        // Act
        var act = () => new ConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*username*secret*");
    }

    [Fact]
    public void Constructor_EmptyPasswordSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret(UserSecretName)).Returns(ValidUser);
        _mockSecretProvider.Setup(s => s.GetSecret(PasswordSecretName)).Returns(string.Empty);

        // Act
        var act = () => new ConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*password*secret*");
    }

    [Fact]
    public void Constructor_ValidSecrets_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret(UserSecretName)).Returns(ValidUser);
        _mockSecretProvider.Setup(s => s.GetSecret(PasswordSecretName)).Returns(ValidPass);

        // Act
        var act = () => new ConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithAdditionalParameters_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AdditionalParameters = "SslMode=Require;Pooling=true";
        _mockSecretProvider.Setup(s => s.GetSecret(UserSecretName)).Returns(ValidUser);
        _mockSecretProvider.Setup(s => s.GetSecret(PasswordSecretName)).Returns(ValidPass);

        // Act
        var act = () => new ConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().NotThrow();
    }
}
