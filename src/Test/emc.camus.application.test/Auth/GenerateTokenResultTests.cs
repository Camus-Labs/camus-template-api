using FluentAssertions;
using emc.camus.application.Auth;

namespace emc.camus.application.test.Auth;

public class GenerateTokenResultTests
{
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly Guid ValidRequestorUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidRequestorUsername = "admin";
    private const string ValidTokenUsername = "admin-token1";

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var token = ValidToken;
        var expiresOn = ValidExpiration;
        var requestorUserId = ValidRequestorUserId;
        var requestorUsername = ValidRequestorUsername;
        var tokenUsername = ValidTokenUsername;

        // Act
        var result = new GenerateTokenResult(token, expiresOn, requestorUserId, requestorUsername, tokenUsername);

        // Assert
        result.Token.Should().Be(token);
        result.ExpiresOn.Should().Be(expiresOn);
        result.RequestorUserId.Should().Be(requestorUserId);
        result.RequestorUsername.Should().Be(requestorUsername);
        result.TokenUsername.Should().Be(tokenUsername);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidToken_ThrowsArgumentException(string? token)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenResult(token!, ValidExpiration, ValidRequestorUserId, ValidRequestorUsername, ValidTokenUsername);

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
        var act = () => new GenerateTokenResult(ValidToken, defaultDate, ValidRequestorUserId, ValidRequestorUsername, ValidTokenUsername);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void Constructor_EmptyRequestorUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var act = () => new GenerateTokenResult(ValidToken, ValidExpiration, emptyGuid, ValidRequestorUsername, ValidTokenUsername);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("requestorUserId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidRequestorUsername_ThrowsArgumentException(string? username)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenResult(ValidToken, ValidExpiration, ValidRequestorUserId, username!, ValidTokenUsername);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("requestorUsername");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTokenUsername_ThrowsArgumentException(string? tokenUsername)
    {
        // Arrange
        // Act
        var act = () => new GenerateTokenResult(ValidToken, ValidExpiration, ValidRequestorUserId, ValidRequestorUsername, tokenUsername!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("tokenUsername");
    }
}
