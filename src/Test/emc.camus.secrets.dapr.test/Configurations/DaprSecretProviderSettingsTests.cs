using emc.camus.secrets.dapr.Configurations;
using FluentAssertions;

namespace emc.camus.secrets.dapr.test.Configurations;

public class DaprSecretProviderSettingsTests
{
    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            TimeoutSeconds = 30,
            SecretNames = new List<string> { "api-key", "db-connection" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyBaseHost_ThrowsArgumentException(string? baseHost)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = baseHost!,
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            SecretNames = new List<string> { "api-key" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("BaseHost cannot be null or empty*")
            .And.ParamName.Should().Be("BaseHost");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyHttpPort_ThrowsArgumentException(string? httpPort)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = httpPort!,
            SecretStoreName = "my-secret-store",
            SecretNames = new List<string> { "api-key" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("HttpPort cannot be null or empty*")
            .And.ParamName.Should().Be("HttpPort");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("65536")]
    public void Validate_WithInvalidHttpPort_ThrowsArgumentException(string httpPort)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = httpPort,
            SecretStoreName = "my-secret-store",
            SecretNames = new List<string> { "api-key" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"HttpPort must be a valid port number (1-65535)*")
            .And.ParamName.Should().Be("HttpPort");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptySecretStoreName_ThrowsArgumentException(string? secretStoreName)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = secretStoreName!,
            SecretNames = new List<string> { "api-key" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("SecretStoreName cannot be null or empty*")
            .And.ParamName.Should().Be("SecretStoreName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_WithInvalidTimeoutSeconds_ThrowsArgumentException(int timeoutSeconds)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            TimeoutSeconds = timeoutSeconds,
            SecretNames = new List<string> { "api-key" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("TimeoutSeconds must be between 1 and 300*")
            .And.ParamName.Should().Be("TimeoutSeconds");
    }

    [Fact]
    public void Validate_WithNullSecretNames_ThrowsArgumentException()
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            SecretNames = null
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("SecretNames cannot be null*")
            .And.ParamName.Should().Be("SecretNames");
    }

    [Fact]
    public void Validate_WithEmptySecretNames_ThrowsArgumentException()
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            SecretNames = new List<string>()
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one secret name must be specified in SecretNames*")
            .And.ParamName.Should().Be("SecretNames");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptySecretName_ThrowsArgumentException(string? secretName)
    {
        // Arrange
        var settings = new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            SecretStoreName = "my-secret-store",
            SecretNames = new List<string> { "api-key", secretName! }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("SecretNames cannot contain null or empty values*")
            .And.ParamName.Should().Be("SecretNames");
    }
}
