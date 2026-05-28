using FluentAssertions;
using emc.camus.domain.Auth;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.domain.test.Auth;

public class AuthTokenTests
{
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider;

    public AuthTokenTests()
    {
        _timeProvider = new FakeTimeProvider(FixedNow);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var token = ValidToken;
        var expiresOn = _timeProvider.GetUtcNow().UtcDateTime.AddYears(1);

        // Act
        var authToken = new AuthToken(token, expiresOn, _timeProvider);

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
        // Act
        var act = () => new AuthToken(token!, _timeProvider.GetUtcNow().UtcDateTime.AddYears(1), _timeProvider);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("token");
    }

    [Fact]
    public void Constructor_PastExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pastDate = _timeProvider.GetUtcNow().UtcDateTime.AddYears(-1);

        // Act
        var act = () => new AuthToken(ValidToken, pastDate, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*greater*")
            .And.ParamName.Should().Be("expiresOn");
    }

}
