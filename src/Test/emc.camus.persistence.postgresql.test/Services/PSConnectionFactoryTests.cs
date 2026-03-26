using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.application.Configurations;
using emc.camus.application.Secrets;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Services;

public class PSConnectionFactoryTests
{
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly Mock<ISecretProvider> _mockSecretProvider = new();

    private static DatabaseSettings CreateValidSettings() => new()
    {
        Host = "localhost",
        Port = 5432,
        Database = "testdb",
        UserSecretName = "db-user",
        PasswordSecretName = "db-password"
    };

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        DatabaseSettings? settings = null;

        // Act
        var act = () => new PSConnectionFactory(settings!, _mockUserContext.Object, _mockSecretProvider.Object);

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
        var act = () => new PSConnectionFactory(settings, null!, _mockSecretProvider.Object);

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
        var act = () => new PSConnectionFactory(settings, _mockUserContext.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("secretProvider");
    }

    [Fact]
    public void Constructor_EmptyUsernameSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret("db-user")).Returns(string.Empty);

        // Act
        var act = () => new PSConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*username*secret*");
    }

    [Fact]
    public void Constructor_EmptyPasswordSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret("db-user")).Returns("validuser");
        _mockSecretProvider.Setup(s => s.GetSecret("db-password")).Returns(string.Empty);

        // Act
        var act = () => new PSConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*password*secret*");
    }

    [Fact]
    public void Constructor_ValidSecrets_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockSecretProvider.Setup(s => s.GetSecret("db-user")).Returns("validuser");
        _mockSecretProvider.Setup(s => s.GetSecret("db-password")).Returns("validpass");

        // Act
        var act = () => new PSConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithAdditionalParameters_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.AdditionalParameters = "SslMode=Require;Pooling=true";
        _mockSecretProvider.Setup(s => s.GetSecret("db-user")).Returns("validuser");
        _mockSecretProvider.Setup(s => s.GetSecret("db-password")).Returns("validpass");

        // Act
        var act = () => new PSConnectionFactory(settings, _mockUserContext.Object, _mockSecretProvider.Object);

        // Assert
        act.Should().NotThrow();
    }
}
