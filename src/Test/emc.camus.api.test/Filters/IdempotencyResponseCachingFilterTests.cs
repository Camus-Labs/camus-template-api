using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using emc.camus.api.Configurations;
using emc.camus.api.Filters;
using emc.camus.api.Metrics;
using emc.camus.api.test.Helpers;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.application.Idempotency;

namespace emc.camus.api.test.Filters;

public class IdempotencyResponseCachingFilterTests : IDisposable
{
    private const string TestIdempotencyKey = "test-key-001";
    private const string TestRequestBody = """{"name":"test"}""";
    private const string DifferentRequestBody = """{"name":"other"}""";

    private static readonly Guid TestUserId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherUserId = new("11111111-2222-3333-4444-555555555555");

    private readonly Mock<IIdempotencyResponseCache> _mockCache;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly IdempotencySettings _settings;
    private readonly IdempotencyMetrics _metrics;
    private readonly Mock<ILogger<IdempotencyResponseCachingFilter>> _mockLogger;
    private readonly ConcurrentBag<(LogLevel Level, string Message)> _logEntries;
    private readonly IdempotencyResponseCachingFilter _filter;

    public IdempotencyResponseCachingFilterTests()
    {
        _mockCache = new Mock<IIdempotencyResponseCache>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(TestUserId);
        _settings = new IdempotencySettings();
        _metrics = new IdempotencyMetrics("test-service");
        (_mockLogger, _logEntries) = LogCaptureBuilder.Create<IdempotencyResponseCachingFilter>();
        _filter = new IdempotencyResponseCachingFilter(
            _mockCache.Object, _mockUserContext.Object, _settings, _metrics, _mockLogger.Object);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ResourceExecutingContext CreateResourceExecutingContext(
        HttpContext httpContext,
        bool hasAttribute = true,
        string policyName = IdempotencyPolicies.Default)
    {
        var actionDescriptor = new ActionDescriptor();

        if (hasAttribute)
        {
            actionDescriptor.EndpointMetadata = new List<object>
            {
                new RequireIdempotencyKeyAttribute(policyName)
            };
        }
        else
        {
            actionDescriptor.EndpointMetadata = new List<object>();
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new ModelStateDictionary());

        return new ResourceExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
    }

    private static DefaultHttpContext CreateHttpContextWithIdempotencyKey(
        string idempotencyKey,
        string body = TestRequestBody)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Headers[Headers.IdempotencyKey] = idempotencyKey;
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        httpContext.Request.ContentType = "application/json";
        return httpContext;
    }

    private static ResourceExecutionDelegate CreateNextDelegate(IActionResult? actionResult = null)
    {
        var result = actionResult ?? new OkObjectResult(new { message = "created" });
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
            {
                Result = result
            };
            return Task.FromResult(context);
        };
    }

    private static ResourceExecutionDelegate CreateNextDelegateWithNonObjectResult()
    {
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
            {
                Result = new EmptyResult()
            };
            return Task.FromResult(context);
        };
    }

    private static ResourceExecutionDelegate CreateNextDelegateThatThrows()
    {
        return () =>
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = new InvalidOperationException("action failed")
            };
            return Task.FromResult(context);
        };
    }

    // --- Constructor validation ---

    public static TheoryData<Func<IdempotencyResponseCachingFilter>, string> NullConstructorArgCases()
    {
        var cache = new Mock<IIdempotencyResponseCache>().Object;
        var userCtx = new Mock<IUserContext>().Object;
        var settings = new IdempotencySettings();
        using var metrics = new IdempotencyMetrics("test-service");
        var logger = new Mock<ILogger<IdempotencyResponseCachingFilter>>().Object;

        return new TheoryData<Func<IdempotencyResponseCachingFilter>, string>
        {
            { () => new IdempotencyResponseCachingFilter(null!, userCtx, settings, metrics, logger), "cache" },
            { () => new IdempotencyResponseCachingFilter(cache, null!, settings, metrics, logger), "userContext" },
            { () => new IdempotencyResponseCachingFilter(cache, userCtx, null!, metrics, logger), "settings" },
            { () => new IdempotencyResponseCachingFilter(cache, userCtx, settings, null!, logger), "metrics" },
            { () => new IdempotencyResponseCachingFilter(cache, userCtx, settings, metrics, null!), "logger" },
        };
    }

    [Theory]
    [MemberData(nameof(NullConstructorArgCases))]
    public void Constructor_NullArg_ThrowsArgumentNullException(
        Func<IdempotencyResponseCachingFilter> create, string expectedParamName)
    {
        // Act
        var act = () => create();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be(expectedParamName);
    }

    // --- AC-01: First request executes action, caches response, returns miss ---

    [Fact]
    public async Task OnResourceExecutionAsync_FirstRequestWithIdempotencyKey_ExecutesActionAndReturnsStatusMiss()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var next = CreateNextDelegate();

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be("miss");
        _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    // --- AC-02: Repeated request with same key, same user, same body returns cached response with hit ---

    [Fact]
    public async Task OnResourceExecutionAsync_RepeatedRequestSameKeyAndBody_ReturnsCachedResponseWithStatusHit()
    {
        // Arrange — cache returns a matching cached response
        var cachedResponse = new CachedResponse(200, "{\"message\":\"created\"}", ComputeHash(TestRequestBody));
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns(cachedResponse);

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var nextWasCalled = false;
        ResourceExecutionDelegate next = () =>
        {
            nextWasCalled = true;
            var ac = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            return Task.FromResult(new ResourceExecutedContext(ac, new List<IFilterMetadata>()));
        };

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be(IdempotencyKeyStatuses.Hit);
        nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnResourceExecutionAsync_CacheHit_ReturnsOriginalStatusCode()
    {
        // Arrange — cache returns response with 201
        var cachedResponse = new CachedResponse(201, "{\"id\":1}", ComputeHash(TestRequestBody));
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns(cachedResponse);

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);

        // Act
        await _filter.OnResourceExecutionAsync(context, CreateNextDelegate());

        // Assert
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(201);
    }

    // --- AC-03: Same key, same user, different body returns 409 ---

    [Fact]
    public async Task OnResourceExecutionAsync_SameKeyDifferentBody_ThrowsDataConflictException()
    {
        // Arrange — cache returns a response with a different body hash
        var cachedResponse = new CachedResponse(200, "{\"name\":\"test\"}", ComputeHash(TestRequestBody));
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns(cachedResponse);

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey, DifferentRequestBody);
        var context = CreateResourceExecutingContext(httpContext);
        var next = CreateNextDelegate();

        // Act
        var act = () => _filter.OnResourceExecutionAsync(context, next);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*idempotency*body*conflict*");
    }

    // --- AC-04: Cached entry expires after TTL ---

    [Fact]
    public async Task OnResourceExecutionAsync_RequestAfterTtlExpiry_TreatedAsNewRequest()
    {
        // Arrange — cache returns null (entry expired)
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);

        // Act
        await _filter.OnResourceExecutionAsync(context, CreateNextDelegate());

        // Assert
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be("miss");
        _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    // --- AC-05: Different users with same idempotency key are independent ---

    [Fact]
    public async Task OnResourceExecutionAsync_DifferentUsersSameKey_TreatedIndependently()
    {
        // Arrange — cache returns null (different user = different key = no entry)
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);

        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns(OtherUserId);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);

        // Act
        await _filter.OnResourceExecutionAsync(context, CreateNextDelegate());

        // Assert — second user gets miss, not hit
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be("miss");
        _mockCache.Verify(c => c.Store(
            It.Is<string>(k => k.StartsWith(OtherUserId.ToString())),
            It.IsAny<CachedResponse>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    // --- AC-06: Responses that cannot be cached are skipped ---

    [Fact]
    public async Task OnResourceExecutionAsync_ActionThrowsException_DoesNotCacheResponse()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);

        // Act
        await _filter.OnResourceExecutionAsync(context, CreateNextDelegateThatThrows());

        // Assert — Store should not be called when exception occurred
        _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task OnResourceExecutionAsync_ActionReturnsNonObjectResult_DoesNotCacheResponse()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);

        // Act
        await _filter.OnResourceExecutionAsync(context, CreateNextDelegateWithNonObjectResult());

        // Assert — Store should not be called for non-ObjectResult responses
        _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()), Times.Never);
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be("miss");
    }

    // --- AC-07: Cache unavailable — fail-open behavior ---

    [Fact]
    public async Task OnResourceExecutionAsync_CacheThrowsException_ProceedsWithoutCaching()
    {
        // Arrange
        var mockCache = new Mock<IIdempotencyResponseCache>();
        mockCache.Setup(c => c.TryGet(It.IsAny<string>()))
            .Throws(new InvalidOperationException("cache unavailable"));

        var failOpenFilter = new IdempotencyResponseCachingFilter(
            mockCache.Object, _mockUserContext.Object, _settings, _metrics, _mockLogger.Object);

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var nextWasCalled = false;
        ResourceExecutionDelegate next = () =>
        {
            nextWasCalled = true;
            var result = new OkObjectResult(new { message = "success" });
            var ac = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            return Task.FromResult(new ResourceExecutedContext(ac, new List<IFilterMetadata>()) { Result = result });
        };

        // Act
        await failOpenFilter.OnResourceExecutionAsync(context, next);

        // Assert
        nextWasCalled.Should().BeTrue();
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains("lookup failed") && e.Message.Contains(TestIdempotencyKey));
    }

    // --- Endpoint without attribute is unaffected ---

    [Fact]
    public async Task OnResourceExecutionAsync_NoRequireIdempotencyKeyAttribute_PassesThroughWithoutCaching()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        var context = CreateResourceExecutingContext(httpContext, hasAttribute: false);
        var nextWasCalled = false;
        ResourceExecutionDelegate next = () =>
        {
            nextWasCalled = true;
            var ac = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            return Task.FromResult(new ResourceExecutedContext(ac, new List<IFilterMetadata>()));
        };

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert
        nextWasCalled.Should().BeTrue();
        httpContext.Response.Headers.Should().NotContainKey(Headers.IdempotencyKeyStatus);
    }

    // --- Unauthenticated user — throws InvalidOperationException ---

    [Fact]
    public async Task OnResourceExecutionAsync_UnauthenticatedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((Guid?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var next = CreateNextDelegate();

        // Act
        var act = () => _filter.OnResourceExecutionAsync(context, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*authenticated*");
    }

    // --- Cache Store failure — fail-open on storage ---

    [Fact]
    public async Task OnResourceExecutionAsync_CacheStoreThrows_ProceedsWithoutError()
    {
        // Arrange — cache lookup returns null (miss), but Store throws
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        _mockCache.Setup(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()))
            .Throws(new InvalidOperationException("storage unavailable"));

        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var next = CreateNextDelegate();

        // Act — should not throw despite storage failure
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert — response still indicates miss; filter did not propagate the error
        httpContext.Response.Headers[Headers.IdempotencyKeyStatus].ToString()
            .Should().Be("miss");
        _logEntries.Should().Contain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains("storage failed") && e.Message.Contains(TestIdempotencyKey));
    }

    // --- LongTerm TTL policy ---

    [Fact]
    public async Task OnResourceExecutionAsync_LongTermPolicy_StoresWithLongTermTtl()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext, hasAttribute: true, policyName: IdempotencyPolicies.LongTerm);
        var next = CreateNextDelegate();

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert — TTL matches LongTermTtlSeconds (default 86400)
        _mockCache.Verify(c => c.Store(
            It.IsAny<string>(),
            It.IsAny<CachedResponse>(),
            TimeSpan.FromSeconds(_settings.LongTermTtlSeconds)), Times.Once);
    }

    // --- ObjectResult with null StatusCode defaults to 200 ---

    [Fact]
    public async Task OnResourceExecutionAsync_ObjectResultWithNullStatusCode_CachesWithStatus200()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        // ObjectResult with no explicit StatusCode set (null)
        var resultWithNullStatus = new ObjectResult(new { id = 42 }) { StatusCode = null };
        var next = CreateNextDelegate(resultWithNullStatus);

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert — cached with 200 as default
        _mockCache.Verify(c => c.Store(
            It.IsAny<string>(),
            It.Is<CachedResponse>(r => r.StatusCode == 200),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    // --- ObjectResult with null Value ---

    [Fact]
    public async Task OnResourceExecutionAsync_ObjectResultWithNullValue_CachesWithNullBody()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        var resultWithNullValue = new ObjectResult(null) { StatusCode = 204 };
        var next = CreateNextDelegate(resultWithNullValue);

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert — cached with null body
        _mockCache.Verify(c => c.Store(
            It.IsAny<string>(),
            It.Is<CachedResponse>(r => r.Body == null),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    // --- Null Result from executed context (not ObjectResult) ---

    [Fact]
    public async Task OnResourceExecutionAsync_NullResult_DoesNotCacheAndLogsWarning()
    {
        // Arrange
        _mockCache.Setup(c => c.TryGet(It.IsAny<string>())).Returns((CachedResponse?)null);
        var httpContext = CreateHttpContextWithIdempotencyKey(TestIdempotencyKey);
        var context = CreateResourceExecutingContext(httpContext);
        ResourceExecutionDelegate next = () =>
        {
            var ac = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            return Task.FromResult(new ResourceExecutedContext(ac, new List<IFilterMetadata>())
            {
                Result = null!
            });
        };

        // Act
        await _filter.OnResourceExecutionAsync(context, next);

        // Assert — Store should not be called for null result
        _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<CachedResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    private static string ComputeHash(string body)
    {
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bodyBytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}
