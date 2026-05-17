using FluentAssertions;
using emc.camus.application.Idempotency;
using emc.camus.cache.inmemory.Services;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.cache.inmemory.test.Services;

public class IdempotencyResponseCacheTests
{
    private const string ValidCompositeKey = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:test-key-001";
    private const string ValidBodyHash = "abc123def456";

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly FakeTimeProvider _timeProvider = new();
    private readonly IdempotencyResponseCache _cache;

    public IdempotencyResponseCacheTests()
    {
        _cache = new IdempotencyResponseCache(_timeProvider);
    }

    // --- TryGet ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGet_InvalidCompositeKey_ThrowsArgumentException(string? compositeKey)
    {
        // Act
        var act = () => _cache.TryGet(compositeKey!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("compositeKey");
    }

    [Fact]
    public void TryGet_KeyNotInCache_ReturnsNull()
    {
        // Act
        var result = _cache.TryGet(ValidCompositeKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_KeyInCacheWithinTtl_ReturnsCachedResponse()
    {
        // Arrange
        var response = new CachedResponse(200, """{"message":"ok"}""", ValidBodyHash);
        _cache.Store(ValidCompositeKey, response, DefaultTtl);

        // Act
        var result = _cache.TryGet(ValidCompositeKey);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Body.Should().Be("""{"message":"ok"}""");
        result.BodyHash.Should().Be(ValidBodyHash);
    }

    [Fact]
    public void TryGet_KeyExpiredBeyondTtl_ReturnsNull()
    {
        // Arrange — store with 1ms TTL, then advance time past expiry
        var response = new CachedResponse(200, """{"message":"ok"}""", ValidBodyHash);
        _cache.Store(ValidCompositeKey, response, TimeSpan.FromMilliseconds(1));

        _timeProvider.Advance(TimeSpan.FromSeconds(1));

        // Act
        var result = _cache.TryGet(ValidCompositeKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGet_DifferentKeysIsolated_ReturnsCorrectEntry()
    {
        // Arrange
        var key1 = "user1:key-a";
        var key2 = "user2:key-a";
        var response1 = new CachedResponse(200, """{"user":"1"}""", "hash1");
        var response2 = new CachedResponse(201, """{"user":"2"}""", "hash2");
        _cache.Store(key1, response1, DefaultTtl);
        _cache.Store(key2, response2, DefaultTtl);

        // Act
        var result1 = _cache.TryGet(key1);
        var result2 = _cache.TryGet(key2);

        // Assert
        result1!.StatusCode.Should().Be(200);
        result2!.StatusCode.Should().Be(201);
    }

    // --- Store ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_InvalidCompositeKey_ThrowsArgumentException(string? compositeKey)
    {
        // Arrange
        var response = new CachedResponse(200, "body", ValidBodyHash);

        // Act
        var act = () => _cache.Store(compositeKey!, response, DefaultTtl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("compositeKey");
    }

    [Fact]
    public void Store_NullResponse_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _cache.Store(ValidCompositeKey, null!, DefaultTtl);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("response");
    }

    [Fact]
    public void Store_ZeroTtl_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var response = new CachedResponse(200, "body", ValidBodyHash);

        // Act
        var act = () => _cache.Store(ValidCompositeKey, response, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("ttl");
    }

    [Fact]
    public void Store_NegativeTtl_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var response = new CachedResponse(200, "body", ValidBodyHash);

        // Act
        var act = () => _cache.Store(ValidCompositeKey, response, TimeSpan.FromSeconds(-1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("ttl");
    }

    [Fact]
    public void Store_OverwritesExistingEntry_ReturnsNewValue()
    {
        // Arrange
        var response1 = new CachedResponse(200, "first", "hash1");
        var response2 = new CachedResponse(201, "second", "hash2");
        _cache.Store(ValidCompositeKey, response1, DefaultTtl);

        // Act
        _cache.Store(ValidCompositeKey, response2, DefaultTtl);
        var result = _cache.TryGet(ValidCompositeKey);

        // Assert
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Be("second");
    }
}
