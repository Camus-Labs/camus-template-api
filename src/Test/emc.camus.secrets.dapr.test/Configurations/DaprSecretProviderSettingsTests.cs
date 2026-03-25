using FluentAssertions;
using emc.camus.secrets.dapr.Configurations;

namespace emc.camus.secrets.dapr.test.Configurations;

public class DaprSecretProviderSettingsTests
{
    private const string ValidBaseHost = "localhost";
    private const string ValidHttpPort = "3500";
    private const string ValidSecretStoreName = "my-secret-store";
    private const int ValidTimeoutSeconds = 30;

    private static DaprSecretProviderSettings CreateValidSettings() => new()
    {
        BaseHost = ValidBaseHost,
        HttpPort = ValidHttpPort,
        SecretStoreName = ValidSecretStoreName,
        TimeoutSeconds = ValidTimeoutSeconds,
        SecretNames = new List<string> { "secret-one" }
    };

    // --- Defaults ---

    [Fact]
    public void Constructor_Defaults_SetsExpectedValues()
    {
        // Arrange
        // Act
        var settings = new DaprSecretProviderSettings();

        // Assert
        settings.BaseHost.Should().Be("localhost");
        settings.HttpPort.Should().Be("3500");
        settings.SecretStoreName.Should().Be("default-secret-store");
        settings.TimeoutSeconds.Should().Be(30);
        settings.SecretNames.Should().BeEmpty();
    }

    [Fact]
    public void Validate_DefaultSettings_ThrowsDueToEmptySecretNames()
    {
        // Arrange
        var settings = new DaprSecretProviderSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*secret*name*specified*");
    }

    // --- Validate Valid ---

    [Fact]
    public void Validate_AllValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MultipleSecretNames_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretNames = new List<string> { "secret-one", "secret-two", "secret-three" };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MinimumPort_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.HttpPort = "1";

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MaximumPort_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.HttpPort = "65535";

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TimeoutSecondsAtMinimum_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.TimeoutSeconds = 1;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TimeoutSecondsAtMaximum_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.TimeoutSeconds = 300;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate BaseHost ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidBaseHost_ThrowsInvalidOperationException(string? baseHost)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.BaseHost = baseHost!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseHost*null*empty*");
    }

    // --- Validate HttpPort ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyHttpPort_ThrowsInvalidOperationException(string? httpPort)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.HttpPort = httpPort!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HttpPort*null*empty*");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("65536")]
    [InlineData("99999")]
    public void Validate_InvalidHttpPort_ThrowsInvalidOperationException(string httpPort)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.HttpPort = httpPort;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HttpPort*valid*port*");
    }

    // --- Validate SecretStoreName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidSecretStoreName_ThrowsInvalidOperationException(string? secretStoreName)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretStoreName = secretStoreName!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretStoreName*null*empty*");
    }

    // --- Validate TimeoutSeconds ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    [InlineData(999)]
    public void Validate_InvalidTimeoutSeconds_ThrowsInvalidOperationException(int timeoutSeconds)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.TimeoutSeconds = timeoutSeconds;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TimeoutSeconds*between*1*300*");
    }

    // --- Validate SecretNames ---

    [Fact]
    public void Validate_NullSecretNames_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretNames = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretNames*null*");
    }

    [Fact]
    public void Validate_EmptySecretNames_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretNames = new List<string>();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*secret*name*specified*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_SecretNamesContainsInvalidEntry_ThrowsInvalidOperationException(string? invalidEntry)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretNames = new List<string> { "valid-secret", invalidEntry! };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretNames*null*empty*");
    }
}
