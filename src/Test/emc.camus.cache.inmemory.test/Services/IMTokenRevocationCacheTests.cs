using FluentAssertions;
using emc.camus.cache.inmemory.Services;

namespace emc.camus.cache.inmemory.test.Services;

public class IMTokenRevocationCacheTests
{
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTime FutureExpiry = new(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IMTokenRevocationCache _cache = new();

    // --- IsRevoked ---

    [Fact]
    public void IsRevoked_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyJti = Guid.Empty;

        // Act
        var act = () => _cache.IsRevoked(emptyJti);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("jti");
    }

    [Fact]
    public void IsRevoked_NonRevokedToken_ReturnsFalse()
    {
        // Arrange
        var jti = ValidJti;

        // Act
        var result = _cache.IsRevoked(jti);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_RevokedTokenWithFutureExpiry_ReturnsTrue()
    {
        // Arrange
        var jti = ValidJti;
        _cache.Revoke(jti, FutureExpiry);

        // Act
        var result = _cache.IsRevoked(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_DifferentTokenNotRevoked_ReturnsFalse()
    {
        // Arrange
        var anotherJti = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _cache.Revoke(ValidJti, FutureExpiry);

        // Act
        var result = _cache.IsRevoked(anotherJti);

        // Assert
        result.Should().BeFalse();
    }

    // --- Revoke ---

    [Fact]
    public void Revoke_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var emptyJti = Guid.Empty;

        // Act
        var act = () => _cache.Revoke(emptyJti, FutureExpiry);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("jti");
    }

    [Fact]
    public void Revoke_DefaultExpiresOn_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var defaultExpiry = default(DateTime);

        // Act
        var act = () => _cache.Revoke(ValidJti, defaultExpiry);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("expiresOn");
    }

    [Fact]
    public void Revoke_PastExpiry_TokenIsNotRevoked()
    {
        // Arrange
        var jti = ValidJti;
        var pastExpiry = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        _cache.Revoke(jti, pastExpiry);

        // Assert
        _cache.IsRevoked(jti).Should().BeFalse();
    }

    [Fact]
    public void Revoke_SameTokenTwice_TokenIsStillRevoked()
    {
        // Arrange
        var jti = ValidJti;
        _cache.Revoke(jti, FutureExpiry);

        // Act
        _cache.Revoke(jti, FutureExpiry);

        // Assert
        _cache.IsRevoked(jti).Should().BeTrue();
    }
}
