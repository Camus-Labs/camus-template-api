using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for Credentials domain entity.
/// Note: Credentials is marked with [ExcludeFromCodeCoverage] as it's a simple DTO.
/// This test provides basic verification of object initialization only.
/// </summary>
public class CredentialsTests
{
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
