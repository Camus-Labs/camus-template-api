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

    [Theory]
    [InlineData("user1:key-a", 200)]
    [InlineData("user2:key-a", 201)]
    public void TryGet_KeyAmongMultiple_ReturnsCorrectEntry(string targetKey, int expectedStatus)
    {
        // Arrange
        _cache.Store("user1:key-a", new CachedResponse(200, """{"user":"1"}""", "hash1"), DefaultTtl);
        _cache.Store("user2:key-a", new CachedResponse(201, """{"user":"2"}""", "hash2"), DefaultTtl);

        // Act
        var result = _cache.TryGet(targetKey);

        // Assert
        result!.StatusCode.Should().Be(expectedStatus);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Store_InvalidTtl_ThrowsArgumentOutOfRangeException(int ttlSeconds)
    {
        // Arrange
        var response = new CachedResponse(200, "body", ValidBodyHash);

        // Act
        var act = () => _cache.Store(ValidCompositeKey, response, TimeSpan.FromSeconds(ttlSeconds));

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

        // Assert
        var result = _cache.TryGet(ValidCompositeKey);
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Be("second");
    }
}
