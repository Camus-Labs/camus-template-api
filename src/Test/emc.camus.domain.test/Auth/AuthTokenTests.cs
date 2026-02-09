using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.domain.test.Auth;

/// <summary>
/// Unit tests for AuthToken domain entity.
/// </summary>
public class AuthTokenTests
{
    [Fact]
    public void Token_ShouldBeSettable()
    {
        // Arrange
        var authToken = new AuthToken();
        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        // Act
        authToken.Token = expectedToken;

        // Assert
        authToken.Token.Should().Be(expectedToken);
    }

    [Fact]
    public void ExpiresOn_ShouldBeSettable()
    {
        // Arrange
        var authToken = new AuthToken();
        var expectedExpiration = DateTime.UtcNow.AddHours(2);

        // Act
        authToken.ExpiresOn = expectedExpiration;

        // Assert
        authToken.ExpiresOn.Should().Be(expectedExpiration);
    }

    [Fact]
    public void Properties_ShouldDefaultToNull()
    {
        // Act
        var authToken = new AuthToken();

        // Assert
        authToken.Token.Should().BeNull();
        authToken.ExpiresOn.Should().Be(default(DateTime));
    }

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
    }

    [Fact]
    public void ExpiresOn_ShouldSupportUtcTime()
    {
        // Arrange & Act
        var authToken = new AuthToken
        {
            ExpiresOn = DateTime.UtcNow.AddHours(2)
        };

        // Assert
        authToken.ExpiresOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Token_CanBeNull()
    {
        // Arrange & Act
        var authToken = new AuthToken
        {
            Token = null
        };

        // Assert
        authToken.Token.Should().BeNull();
    }

    [Fact]
    public void Token_CanBeEmpty()
    {
        // Arrange & Act
        var authToken = new AuthToken
        {
            Token = string.Empty
        };

        // Assert
        authToken.Token.Should().BeEmpty();
    }
}
