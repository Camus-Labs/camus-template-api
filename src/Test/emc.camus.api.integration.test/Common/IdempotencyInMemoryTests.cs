using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Common;
using emc.camus.application.Idempotency;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

/// <summary>
/// Integration tests for the idempotency feature through the full HTTP pipeline.
/// Proves cross-layer collaboration: validation filter, caching filter, cache adapter,
/// exception handling middleware, and authentication middleware.
/// Permutation-level scenarios are covered by unit tests — these tests verify wiring only.
/// Uses <see cref="IdempotencyTestController"/> registered in <see cref="ApiInMemoryFactory"/>.
/// </summary>
[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class IdempotencyInMemoryTests
{
    private readonly ApiInMemoryFactory _factory;

    private const string DecoratedWithBodyEndpoint = "/api/v1/test/idempotency/decorated-with-body";

    public IdempotencyInMemoryTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task PostToUndecoratedEndpoint_NoIdempotencyKeyHeader_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/v1/test/idempotency/undecorated", null, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostWithIdempotencyKey_RepeatedRequestSameBody_ReturnsMissThenHit()
    {
        // Arrange
        var client = _factory.CreateJwtClient();
        var payload = new { Value = "cached-body" };

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        firstRequest.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "cache-hit-key");
        firstRequest.Content = JsonContent.Create(payload);
        var firstResponse = await client.SendAsync(firstRequest, TestContext.Current.CancellationToken);
        await firstResponse.EnsureSetupSuccessAsync("first request must seed the idempotency cache");
        firstResponse.Headers.GetValues(Headers.IdempotencyKeyStatus).Should().ContainSingle()
            .Which.Should().Be(IdempotencyKeyStatuses.Miss, "precondition: first request must be a cache miss");

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        secondRequest.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "cache-hit-key");
        secondRequest.Content = JsonContent.Create(payload);

        // Act
        var response = await client.SendAsync(secondRequest, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        response.Headers.GetValues(Headers.IdempotencyKeyStatus).Should().ContainSingle()
            .Which.Should().Be(IdempotencyKeyStatuses.Hit);
    }

    [Fact]
    public async Task PostWithIdempotencyKey_TwoUsersSameKey_EachRetrievesOwnCachedResponse()
    {
        // Arrange
        var userAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var clientA = _factory.CreateJwtClient(userAId, "user-a");
        var clientB = _factory.CreateJwtClient(userBId, "user-b");
        var payloadA = new { Value = "response-a" };
        var payloadB = new { Value = "response-b" };

        using var firstRequestA = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        firstRequestA.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "shared-key");
        firstRequestA.Content = JsonContent.Create(payloadA);
        await clientA.SendAsync(firstRequestA, TestContext.Current.CancellationToken);

        using var firstRequestB = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        firstRequestB.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "shared-key");
        firstRequestB.Content = JsonContent.Create(payloadB);
        await clientB.SendAsync(firstRequestB, TestContext.Current.CancellationToken);

        using var secondRequestA = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        secondRequestA.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "shared-key");
        secondRequestA.Content = JsonContent.Create(payloadA);

        using var secondRequestB = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        secondRequestB.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "shared-key");
        secondRequestB.Content = JsonContent.Create(payloadB);

        // Act
        var responseA = await clientA.SendAsync(secondRequestA, TestContext.Current.CancellationToken);
        var responseB = await clientB.SendAsync(secondRequestB, TestContext.Current.CancellationToken);

        // Assert — User A retrieves their own cached response
        await responseA.Should().HaveStatusCode(HttpStatusCode.OK);
        responseA.Headers.GetValues(Headers.IdempotencyKeyStatus).Should().ContainSingle()
            .Which.Should().Be(IdempotencyKeyStatuses.Hit);
        var bodyA = await responseA.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        bodyA.GetProperty("value").GetString().Should().Be("response-a");

        // Assert — User B also retrieves their own cached response (independent verification)
        await responseB.Should().HaveStatusCode(HttpStatusCode.OK);
        responseB.Headers.GetValues(Headers.IdempotencyKeyStatus).Should().ContainSingle()
            .Which.Should().Be(IdempotencyKeyStatuses.Hit);
        var bodyB = await responseB.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        bodyB.GetProperty("value").GetString().Should().Be("response-b");
    }

    [Fact]
    public async Task PostWithIdempotencyKey_SameKeyDifferentBody_Returns409WithBodyConflictErrorCode()
    {
        // Arrange
        var client = _factory.CreateJwtClient();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        firstRequest.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "body-conflict-key");
        firstRequest.Content = JsonContent.Create(new { Value = "original" });
        await client.SendAsync(firstRequest, TestContext.Current.CancellationToken);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        secondRequest.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "body-conflict-key");
        secondRequest.Content = JsonContent.Create(new { Value = "different" });

        // Act
        var response = await client.SendAsync(secondRequest, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        await response.Should().HaveErrorCode(ErrorCodes.IdempotencyBodyConflict);
    }

    [Fact]
    public async Task PostWithIdempotencyKey_UnauthenticatedUser_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, DecoratedWithBodyEndpoint);
        request.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "unauthenticated-key");
        request.Content = JsonContent.Create(new { Value = "unauthenticated" });

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode(ErrorCodes.JwtAuthenticationRequired);
    }
}
