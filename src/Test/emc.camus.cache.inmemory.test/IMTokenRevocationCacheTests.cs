using emc.camus.cache.inmemory.Caches;
using FluentAssertions;

namespace emc.camus.cache.inmemory.test;

/// <summary>
/// Unit tests for <see cref="IMTokenRevocationCache"/>.
/// Validates thread-safe revocation, expiration-based eviction, and edge cases.
/// </summary>
public class IMTokenRevocationCacheTests
{
    private readonly IMTokenRevocationCache _cache = new();

    [Fact]
    public void IsRevoked_WithNonRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        var jti = Guid.NewGuid();

        // Act
        var result = _cache.IsRevoked(jti);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WithRevokedToken_ShouldReturnTrue()
    {
        // Arrange
        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddHours(1);
        _cache.Revoke(jti, expiresOn);

        // Act
        var result = _cache.IsRevoked(jti);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_WithExpiredRevokedToken_ShouldReturnFalse_AndEvict()
    {
        // Arrange
        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddMilliseconds(-1);
        _cache.Revoke(jti, expiresOn);

        // Act
        var result = _cache.IsRevoked(jti);

        // Assert — expired token should not be considered revoked
        result.Should().BeFalse();

        // Second call should also return false (entry was evicted)
        _cache.IsRevoked(jti).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithExpiredToken_ShouldNotAddToCache()
    {
        // Arrange
        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddMilliseconds(-1);

        // Act
        _cache.Revoke(jti, expiresOn);

        // Assert — expired token should not be in cache
        _cache.IsRevoked(jti).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithSameJtiTwice_ShouldNotThrow()
    {
        // Arrange
        var jti = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddHours(1);

        // Act — revoking same token twice should be idempotent
        var act = () =>
        {
            _cache.Revoke(jti, expiresOn);
            _cache.Revoke(jti, expiresOn);
        };

        // Assert
        act.Should().NotThrow();
        _cache.IsRevoked(jti).Should().BeTrue();
    }

    [Fact]
    public void Revoke_WithMultipleTokens_ShouldTrackEachIndependently()
    {
        // Arrange
        var jti1 = Guid.NewGuid();
        var jti2 = Guid.NewGuid();
        var jti3 = Guid.NewGuid();

        // Act — revoke only jti1 and jti2
        _cache.Revoke(jti1, DateTime.UtcNow.AddHours(1));
        _cache.Revoke(jti2, DateTime.UtcNow.AddHours(2));

        // Assert
        _cache.IsRevoked(jti1).Should().BeTrue();
        _cache.IsRevoked(jti2).Should().BeTrue();
        _cache.IsRevoked(jti3).Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_ShouldBeThreadSafe()
    {
        // Arrange
        var jtis = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var expiresOn = DateTime.UtcNow.AddHours(1);

        // Act — revoke and check concurrently
        Parallel.ForEach(jtis, jti => _cache.Revoke(jti, expiresOn));

        // Assert — all should be revoked
        foreach (var jti in jtis)
        {
            _cache.IsRevoked(jti).Should().BeTrue();
        }
    }
}
