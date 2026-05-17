using System.Globalization;
using System.Net;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Common;
using emc.camus.application.RateLimiting;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

/// <summary>
/// Integration tests for IP-based rate limit partitioning.
/// Verifies that the sliding window rate limiter correctly throttles requests per IP address
/// and that different IPs maintain independent rate limit buckets through the full HTTP pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Collection(RateLimitingTestGroup.Name)]
public class RateLimitingIpPartitionTests
{
    private readonly ApiRateLimitingFactory _factory;

    private const string RelaxedEndpoint = "/api/v1/apiinfo/info";
    private const string DefaultEndpoint = "/api/v2/apiinfo/info-jwt";
    private const string StrictEndpoint = "/api/v2/auth/authenticate";

    public RateLimitingIpPartitionTests(ApiRateLimitingFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task SameIp_ExceedsPermitLimit_Returns429WithErrorCodeAndHeaders()
    {
        // Arrange
        var client = _factory.CreateClient().WithIp("10.0.0.1");
        var ct = TestContext.Current.CancellationToken;
        await RateLimitHelper.ExhaustRateLimitWithAssertAsync(client, RelaxedEndpoint, ApiRateLimitingFactory.RelaxedPolicyPermitLimit, ct);

        // Act
        var rejectedResponse = await client.GetAsync(RelaxedEndpoint, ct);

        // Assert
        await rejectedResponse.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await rejectedResponse.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
        rejectedResponse.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.RelaxedPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        rejectedResponse.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Relaxed);
        rejectedResponse.Headers.GetValues("Retry-After").Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DifferentIps_SameEndpoint_HaveIndependentRateLimitBuckets()
    {
        // Arrange
        var clientA = _factory.CreateClient().WithIp("10.1.0.1");
        var clientB = _factory.CreateClient().WithIp("10.1.0.2");
        var ct = TestContext.Current.CancellationToken;
        await RateLimitHelper.ExhaustRateLimitAsync(clientA, RelaxedEndpoint, ApiRateLimitingFactory.RelaxedPolicyPermitLimit, ct);

        // Act
        var responseBFirst = await clientB.GetAsync(RelaxedEndpoint, ct);
        var responseAExtra = await clientA.GetAsync(RelaxedEndpoint, ct);

        // Assert — IP B is independent, still within its own budget
        await responseBFirst.Should().HaveStatusCode(HttpStatusCode.OK);

        // Assert — IP A is still throttled
        await responseAExtra.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await responseAExtra.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
    }

    [Fact]
    public async Task DifferentIps_BothExhaustLimitsIndependently_BothGetThrottled()
    {
        // Arrange
        var clientA = _factory.CreateClient().WithIp("10.2.0.1");
        var clientB = _factory.CreateClient().WithIp("10.2.0.2");
        var ct = TestContext.Current.CancellationToken;

        // Exhaust both IPs' rate limits
        await RateLimitHelper.ExhaustRateLimitForBothAsync(clientA, clientB, RelaxedEndpoint, ApiRateLimitingFactory.RelaxedPolicyPermitLimit, ct);

        // Act
        var responseA = await clientA.GetAsync(RelaxedEndpoint, ct);
        var responseB = await clientB.GetAsync(RelaxedEndpoint, ct);

        // Assert — both are throttled independently
        await responseA.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await responseA.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
        await responseB.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await responseB.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
    }

    [Fact]
    public async Task RelaxedPolicy_ThrottlesAtRelaxedLimit_NotDefaultOrStrictLimit()
    {
        // Arrange
        var client = _factory.CreateClient().WithIp("10.4.0.1");
        var ct = TestContext.Current.CancellationToken;
        await RateLimitHelper.ExhaustRateLimitWithAssertAsync(client, RelaxedEndpoint, ApiRateLimitingFactory.RelaxedPolicyPermitLimit, ct);

        // Act
        var rejectedResponse = await client.GetAsync(RelaxedEndpoint, ct);

        // Assert — throttled at relaxed limit, response confirms relaxed policy
        await rejectedResponse.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await rejectedResponse.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
        rejectedResponse.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.RelaxedPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        rejectedResponse.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Relaxed);
        rejectedResponse.Headers.GetValues("Retry-After").Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DefaultPolicy_ThrottlesAtDefaultLimit_NotRelaxedOrStrictLimit()
    {
        // Arrange
        var client = _factory.CreateJwtClient().WithIp("10.5.0.1");
        var ct = TestContext.Current.CancellationToken;
        await RateLimitHelper.ExhaustRateLimitWithAssertAsync(client, DefaultEndpoint, ApiRateLimitingFactory.DefaultPolicyPermitLimit, ct);

        // Act
        var rejectedResponse = await client.GetAsync(DefaultEndpoint, ct);

        // Assert — throttled at default limit, response confirms default policy
        await rejectedResponse.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await rejectedResponse.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
        rejectedResponse.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.DefaultPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        rejectedResponse.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Default);
        rejectedResponse.Headers.GetValues("Retry-After").Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task StrictPolicy_ThrottlesAtStrictLimit_LowerThanOtherPolicies()
    {
        // Arrange — API key-authenticated client hitting a strict-policy endpoint (auth)
        var client = _factory.CreateApiKeyClient().WithIp("10.6.0.1");
        var ct = TestContext.Current.CancellationToken;

        // Arrange — exhaust permits up to the strict limit (expect 400 since we're posting empty body,
        // but rate limiting happens before action execution so the request still consumes a permit)
        await RateLimitHelper.ExhaustRateLimitAsync(client, StrictEndpoint, ApiRateLimitingFactory.StrictPolicyPermitLimit, ct, HttpMethod.Post);

        // Act — next request exceeds the strict limit
        var rejectedResponse = await client.PostAsync(StrictEndpoint, null, ct);

        // Assert — throttled at strict limit (lowest of all policies)
        await rejectedResponse.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
        await rejectedResponse.Should().HaveErrorCode(ErrorCodes.RateLimitExceeded);
        rejectedResponse.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.StrictPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        rejectedResponse.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Strict);
        rejectedResponse.Headers.GetValues("Retry-After").Should().ContainSingle()
            .Which.Should().Be(ApiRateLimitingFactory.PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SameIp_AfterWindowResets_PermitsAreReplenished()
    {
        // Arrange
        var client = _factory.CreateClient().WithIp("10.7.0.1");
        var ct = TestContext.Current.CancellationToken;

        // Exhaust the rate limit
        await RateLimitHelper.ExhaustRateLimitAsync(client, RelaxedEndpoint, ApiRateLimitingFactory.RelaxedPolicyPermitLimit, ct);

        // Verify the limit is actually exhausted
        var rejectedResponse = await client.GetAsync(RelaxedEndpoint, ct);
        await rejectedResponse.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);

        // Act — wait for the sliding window to fully expire
        await Task.Delay(TimeSpan.FromSeconds(ApiRateLimitingFactory.PolicyWindowSeconds + 1), ct);

        // Assert — permits are replenished after window reset
        var response = await client.GetAsync(RelaxedEndpoint, ct);
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

}
