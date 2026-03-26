using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class AuthenticateUserResultTests
{
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var token = ValidToken;
        var expiresOn = ValidExpiration;

        // Act
        var result = new AuthenticateUserResult(token, expiresOn);

        // Assert
        result.Token.Should().Be(token);
        result.ExpiresOn.Should().Be(expiresOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidToken_ThrowsArgumentException(string? token)
    {
        // Arrange
        // Act
        var act = () => new AuthenticateUserResult(token!, ValidExpiration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("token");
    }

    [Fact]
    public void Constructor_DefaultExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultDate = default(DateTime);

        // Act
        var act = () => new AuthenticateUserResult(ValidToken, defaultDate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }
}
