using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for AuthToken domain entity.
/// Note: AuthToken is marked with [ExcludeFromCodeCoverage] as it's a simple DTO.
/// This test provides basic verification of object initialization only.
/// </summary>
public class AuthTokenTests
{
    [Fact]
    public void AuthToken_WithValidValues_ShouldStoreCorrectly()
    {
        // Arrange
        var expectedToken = "fake-jwt-token";
        var expectedExpiration = DateTime.UtcNow.AddHours(1);

        // Act
        var authToken = new AuthToken
        {
            Token = expectedToken,
            ExpiresOn = expectedExpiration
        };

        // Assert
        authToken.Token.Should().Be(expectedToken);
        authToken.ExpiresOn.Should().Be(expectedExpiration);
        authToken.ExpiresOn.Kind.Should().Be(DateTimeKind.Utc);
    }
}
