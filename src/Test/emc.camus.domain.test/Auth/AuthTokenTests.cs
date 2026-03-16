using FluentAssertions;
using emc.camus.domain.Auth;

namespace emc.camus.domain.test.Auth;

public class AuthTokenTests
{
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTime FutureExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime PastExpiration = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var token = ValidToken;
        var expiresOn = FutureExpiration;

        // Act
        var authToken = new AuthToken(token, expiresOn);

        // Assert
        authToken.Token.Should().Be(token);
        authToken.ExpiresOn.Should().Be(expiresOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidToken_ThrowsArgumentException(string? token)
    {
        // Arrange
        // Act
        var act = () => new AuthToken(token!, FutureExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("token");
    }

    [Fact]
    public void Constructor_PastExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pastDate = PastExpiration;

        // Act
        var act = () => new AuthToken(ValidToken, pastDate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*greater*")
            .And.ParamName.Should().Be("expiresOn");
    }

}
