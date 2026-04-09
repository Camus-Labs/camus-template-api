using FluentAssertions;
using emc.camus.cache.inmemory.Services;

namespace emc.camus.cache.inmemory.test.Services;

public class IMTokenRevocationCacheTests
{
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
    public void IsRevoked_RevokedToken_ReturnsTrue()
    {
        // Arrange
        var jti = ValidJti;
        _cache.Revoke(jti);

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
        _cache.Revoke(ValidJti);

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
        var act = () => _cache.Revoke(emptyJti);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*equal*")
            .And.ParamName.Should().Be("jti");
    }

    [Fact]
    public void Revoke_SameTokenTwice_TokenIsStillRevoked()
    {
        // Arrange
        var jti = ValidJti;
        _cache.Revoke(jti);

        // Act
        _cache.Revoke(jti);

        // Assert
        _cache.IsRevoked(jti).Should().BeTrue();
    }

    // --- Refresh ---

    [Fact]
    public void Refresh_NullSet_ThrowsArgumentNullException()
    {
        // Arrange
        HashSet<Guid> revokedJtis = null!;

        // Act
        var act = () => _cache.Refresh(revokedJtis);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("revokedJtis");
    }

    [Fact]
    public void Refresh_EmptySet_ClearsExistingEntries()
    {
        // Arrange
        _cache.Revoke(ValidJti);
        _cache.IsRevoked(ValidJti).Should().BeTrue("precondition: token should be revoked");

        // Act
        _cache.Refresh([]);

        // Assert
        _cache.IsRevoked(ValidJti).Should().BeFalse();
    }

    [Fact]
    public void Refresh_WithEntries_ReplacesExistingCache()
    {
        // Arrange — cache has one token, replacement has a different one
        _cache.Revoke(ValidJti);
        var replacementJti = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // Act
        _cache.Refresh([replacementJti]);

        // Assert
        _cache.IsRevoked(ValidJti).Should().BeFalse("old entry should be removed");
        _cache.IsRevoked(replacementJti).Should().BeTrue("new entry should be present");
    }
}
