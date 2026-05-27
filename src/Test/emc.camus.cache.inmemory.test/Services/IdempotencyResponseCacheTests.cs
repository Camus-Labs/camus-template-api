using FluentAssertions;
using emc.camus.application.Idempotency;
using emc.camus.cache.inmemory.Services;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.cache.inmemory.test.Services;

public class IdempotencyResponseCacheTests
{
    private const string ValidCompositeKey = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:test-key-001";
    private const string ValidBodyHash = "abc123def456";
    private const string User1Key = "user1:key-a";
    private const string User2Key = "user2:key-a";
    private const string Hash1 = "hash1";
    private const string Hash2 = "hash2";
    private const int DefaultStatusCode = 200;
    private const string DefaultBody = "body";

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly FakeTimeProvider _timeProvider;
    private readonly IdempotencyResponseCache _cache;

    public IdempotencyResponseCacheTests()
    {
        _timeProvider = new FakeTimeProvider();
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
    [InlineData(User1Key, 200)]
    [InlineData(User2Key, 201)]
    public void TryGet_KeyAmongMultiple_ReturnsCorrectEntry(string targetKey, int expectedStatus)
    {
        // Arrange
        _cache.Store(User1Key, new CachedResponse(200, """{"user":"1"}""", Hash1), DefaultTtl);
        _cache.Store(User2Key, new CachedResponse(201, """{"user":"2"}""", Hash2), DefaultTtl);

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
        var response = new CachedResponse(DefaultStatusCode, DefaultBody, ValidBodyHash);

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
        var response = new CachedResponse(DefaultStatusCode, DefaultBody, ValidBodyHash);

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
        var response1 = new CachedResponse(200, "first", Hash1);
        var response2 = new CachedResponse(201, "second", Hash2);
        _cache.Store(ValidCompositeKey, response1, DefaultTtl);

        // Act
        _cache.Store(ValidCompositeKey, response2, DefaultTtl);

        // Assert
        var result = _cache.TryGet(ValidCompositeKey);
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Be("second");
    }
}
