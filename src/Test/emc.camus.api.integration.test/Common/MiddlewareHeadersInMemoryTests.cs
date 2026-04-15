using System.Net;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Common;
using emc.camus.application.RateLimiting;
using FluentAssertions;
using Microsoft.Net.Http.Headers;

namespace emc.camus.api.integration.test.Common;

[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class MiddlewareHeadersInMemoryTests
{
    private readonly ApiInMemoryFactory _factory;

    private const string ExpectedPermitLimit = "10000";
    private const string ExpectedWindowSeconds = "60";

    public MiddlewareHeadersInMemoryTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAnonymousUsername()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1.0/apiinfo/info", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        response.Headers.GetValues(HeaderNames.XContentTypeOptions).Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues(HeaderNames.XFrameOptions).Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues(HeaderNames.XXSSProtection).Should().ContainSingle().Which.Should().Be("1; mode=block");
        response.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Should().ContainSingle()
            .Which.Should().Contain("default-src 'self'");
        response.Headers.GetValues(Headers.TraceId).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ExpectedPermitLimit);
        response.Headers.GetValues(Headers.RateLimitReset).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Relaxed);
        response.Headers.GetValues(Headers.RateLimitWindow).Should().ContainSingle()
            .Which.Should().Be(ExpectedWindowSeconds);
        response.Headers.GetValues(Headers.Username).Should().ContainSingle()
            .Which.Should().Be("anonymous");
    }

    [Fact]
    public async Task AuthenticatedJwtRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndAuthenticatedUsername()
    {
        // Arrange
        var client = _factory.CreateJwtClient();

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        response.Headers.GetValues(HeaderNames.XContentTypeOptions).Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues(HeaderNames.XFrameOptions).Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues(HeaderNames.XXSSProtection).Should().ContainSingle().Which.Should().Be("1; mode=block");
        response.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Should().ContainSingle()
            .Which.Should().Contain("default-src 'self'");
        response.Headers.GetValues(Headers.TraceId).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ExpectedPermitLimit);
        response.Headers.GetValues(Headers.RateLimitReset).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Default);
        response.Headers.GetValues(Headers.RateLimitWindow).Should().ContainSingle()
            .Which.Should().Be(ExpectedWindowSeconds);
        response.Headers.GetValues(Headers.Username).Should().ContainSingle()
            .Which.Should().Be("test-user");
    }

    [Fact]
    public async Task AuthenticatedApiKeyRequest_ResponseHeaders_ContainSecurityTraceRateLimitAndApiKeyUsername()
    {
        // Arrange
        var client = _factory.CreateApiKeyClient();

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-apikey", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        response.Headers.GetValues(HeaderNames.XContentTypeOptions).Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues(HeaderNames.XFrameOptions).Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues(HeaderNames.XXSSProtection).Should().ContainSingle().Which.Should().Be("1; mode=block");
        response.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Should().ContainSingle()
            .Which.Should().Contain("default-src 'self'");
        response.Headers.GetValues(Headers.TraceId).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ExpectedPermitLimit);
        response.Headers.GetValues(Headers.RateLimitReset).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Default);
        response.Headers.GetValues(Headers.RateLimitWindow).Should().ContainSingle()
            .Which.Should().Be(ExpectedWindowSeconds);
        response.Headers.GetValues(Headers.Username).Should().ContainSingle()
            .Which.Should().Be("ApiKeyUser");
    }

    [Fact]
    public async Task UnauthorizedRequest_ResponseHeaders_ContainSecurityRateLimitAndTraceButNoUsername()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);

        response.Headers.GetValues(HeaderNames.XContentTypeOptions).Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues(HeaderNames.XFrameOptions).Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues(HeaderNames.XXSSProtection).Should().ContainSingle().Which.Should().Be("1; mode=block");
        response.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Should().ContainSingle()
            .Which.Should().Contain("default-src 'self'");
        response.Headers.GetValues(Headers.TraceId).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitLimit).Should().ContainSingle()
            .Which.Should().Be(ExpectedPermitLimit);
        response.Headers.GetValues(Headers.RateLimitReset).Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(Headers.RateLimitPolicy).Should().ContainSingle()
            .Which.Should().Be(RateLimitPolicies.Default);
        response.Headers.GetValues(Headers.RateLimitWindow).Should().ContainSingle()
            .Which.Should().Be(ExpectedWindowSeconds);
        response.Headers.Should().NotContainKey(Headers.Username);
    }
}
