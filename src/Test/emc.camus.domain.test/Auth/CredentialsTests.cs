using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for Credentials domain entity.
/// </summary>
public class CredentialsTests
{
    [Fact]
    public void AccessKey_ShouldBeSettable()
    {
        // Arrange
        var credentials = new Credentials();
        var expectedKey = "test-access-key";

        // Act
        credentials.AccessKey = expectedKey;

        // Assert
        credentials.AccessKey.Should().Be(expectedKey);
    }

    [Fact]
    public void AccessSecret_ShouldBeSettable()
    {
        // Arrange
        var credentials = new Credentials();
        var expectedSecret = "test-access-secret";

        // Act
        credentials.AccessSecret = expectedSecret;

        // Assert
        credentials.AccessSecret.Should().Be(expectedSecret);
    }

    [Fact]
    public void Properties_ShouldDefaultToNull()
    {
        // Act
        var credentials = new Credentials();

        // Assert
        credentials.AccessKey.Should().BeNull();
        credentials.AccessSecret.Should().BeNull();
    }

    [Fact]
    public void Credentials_ShouldAllowNullValues()
    {
        // Arrange & Act
        var credentials = new Credentials
        {
            AccessKey = null,
            AccessSecret = null
        };

        // Assert
        credentials.AccessKey.Should().BeNull();
        credentials.AccessSecret.Should().BeNull();
    }

    [Fact]
    public void Credentials_ShouldAllowEmptyStrings()
    {
        // Arrange & Act
        var credentials = new Credentials
        {
            AccessKey = string.Empty,
            AccessSecret = string.Empty
        };

        // Assert
        credentials.AccessKey.Should().BeEmpty();
        credentials.AccessSecret.Should().BeEmpty();
    }

    [Fact]
    public void Credentials_WithValidValues_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var credentials = new Credentials
        {
            AccessKey = "my-access-key",
            AccessSecret = "my-secret-key"
        };

        // Assert
        credentials.AccessKey.Should().Be("my-access-key");
        credentials.AccessSecret.Should().Be("my-secret-key");
    }
}
