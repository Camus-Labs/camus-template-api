using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Common;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

/// <summary>
/// Integration tests for the idempotency key validation filter through the full HTTP pipeline.
/// Verifies that the <c>[RequireIdempotencyKey]</c> attribute triggers header validation on decorated
/// endpoints and that non-decorated endpoints are unaffected.
/// Uses <see cref="IdempotencyTestController"/> registered in <see cref="ApiInMemoryFactory"/>.
/// </summary>
[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class IdempotencyKeyValidationInMemoryTests
{
    private readonly ApiInMemoryFactory _factory;

    private const string DecoratedEndpoint = "/api/v1/test/idempotency/decorated";
    private const string UndecoratedEndpoint = "/api/v1/test/idempotency/undecorated";

    public IdempotencyKeyValidationInMemoryTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task PostToDecoratedEndpoint_MissingIdempotencyKeyHeader_Returns400WithMissingErrorCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(DecoratedEndpoint, null, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveErrorCode(ErrorCodes.IdempotencyKeyMissing);
    }

    [Fact]
    public async Task PostToDecoratedEndpoint_ValidIdempotencyKey_Returns200WithBody()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, DecoratedEndpoint);
        request.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, "test-key-123");

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task PostToUndecoratedEndpoint_NoIdempotencyKeyHeader_Returns200WithBody()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(UndecoratedEndpoint, null, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("status").GetString().Should().Be("ok");
    }
}
