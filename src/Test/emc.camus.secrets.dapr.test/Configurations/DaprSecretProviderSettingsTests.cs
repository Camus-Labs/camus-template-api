using FluentAssertions;
using emc.camus.secrets.dapr.Configurations;
using static emc.camus.secrets.dapr.test.Helpers.DaprSecretProviderSettingsBuilder;

namespace emc.camus.secrets.dapr.test.Configurations;

public class DaprSecretProviderSettingsTests
{
    private const string EmptyString = "";
    private const string WhitespaceOnly = "   ";
    private static readonly List<string> MultipleSecretNames = new() { "secret-one", "secret-two", "secret-three" };
    private static readonly List<string> EmptySecretNamesList = new();
    private static readonly List<string> DefaultSecretNames = new() { "secret-one" };
    public static readonly TheoryData<List<string>> InvalidSecretNameEntries = new()
    {
        { new List<string> { "valid-secret", null! } },
        { new List<string> { "valid-secret", EmptyString } },
        { new List<string> { "valid-secret", WhitespaceOnly } }
    };

    private static DaprSecretProviderSettings CreateValidSettings() => CreateValid(
        secretNames: DefaultSecretNames);

    // --- Defaults ---

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
        settings.SecretNames = MultipleSecretNames;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("65535")]
    public void Validate_BoundaryPort_DoesNotThrow(string httpPort)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.HttpPort = httpPort;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(300)]
    public void Validate_BoundaryTimeoutSeconds_DoesNotThrow(int timeoutSeconds)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.TimeoutSeconds = timeoutSeconds;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate BaseHost ---

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
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
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
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
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
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
        settings.SecretNames = EmptySecretNamesList;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*secret*name*specified*");
    }

    [Theory]
    [MemberData(nameof(InvalidSecretNameEntries))]
    public void Validate_SecretNamesContainsInvalidEntry_ThrowsInvalidOperationException(List<string> secretNames)
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.SecretNames = secretNames;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretNames*null*empty*");
    }
}
