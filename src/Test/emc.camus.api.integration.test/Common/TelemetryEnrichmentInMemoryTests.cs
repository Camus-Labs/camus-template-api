using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Common;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class TelemetryEnrichmentInMemoryTests
{
    private readonly ApiInMemoryFactory _factory;

    private static readonly Guid TestUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string TestUsername = "test-user";

    public TelemetryEnrichmentInMemoryTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousRequest_TelemetryTags_EnduserAuthenticatedIsFalseAndNoIdentityTags()
    {
        // Arrange
        using var capture = new ActivityCapture();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo/info", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var activity = capture.GetSingleByRoute("ApiInfo/info");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("enduser.authenticated").Should().Be(false);
        activity.GetTagItem("enduser.name").Should().BeNull();
        activity.GetTagItem("enduser.id").Should().BeNull();
        activity.GetTagItem("http.route.controller").Should().Be("ApiInfo");
        activity.GetTagItem("http.route.version").Should().Be("1");

        var innerActivity = capture.GetSingleByDisplayName("GetInfo");
        innerActivity.GetTagItem("operation.type").Should().Be("read");
        innerActivity.GetTagItem("otel.status_code").Should().Be("OK");
    }

    [Fact]
    public async Task ApiKeyAuthenticatedRequest_TelemetryTags_EnduserAuthenticatedIsTrueWithoutIdentityTags()
    {
        // Arrange
        using var capture = new ActivityCapture();
        var client = _factory.CreateApiKeyClient();

        // Act
        var response = await client.GetAsync("/api/v2/apiinfo/info-apikey", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var activity = capture.GetSingleByRoute("ApiInfo/info-apikey");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("enduser.authenticated").Should().Be(true);
        activity.GetTagItem("enduser.name").Should().Be("ApiKeyUser");
        activity.GetTagItem("enduser.id").Should().Be("00000000-0000-0000-0000-000000000001", "API key identity uses a deterministic NameIdentifier");
        activity.GetTagItem("http.route.controller").Should().Be("ApiInfo");
        activity.GetTagItem("http.route.version").Should().Be("2");

        var innerActivity = capture.GetSingleByDisplayName("GetInfoApiKey");
        innerActivity.GetTagItem("operation.type").Should().Be("read");
        innerActivity.GetTagItem("otel.status_code").Should().Be("OK");
    }

    [Fact]
    public async Task JwtAuthenticatedRequest_TelemetryTags_EnduserAuthenticatedIsTrueWithNameAndId()
    {
        // Arrange
        using var capture = new ActivityCapture();
        var client = _factory.CreateJwtClient(TestUserId, TestUsername);

        // Act
        var response = await client.GetAsync("/api/v2/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var activity = capture.GetSingleByRoute("ApiInfo/info-jwt");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("enduser.authenticated").Should().Be(true);
        activity.GetTagItem("enduser.name").Should().Be(TestUsername);
        activity.GetTagItem("enduser.id").Should().Be(TestUserId.ToString());
        activity.GetTagItem("http.route.controller").Should().Be("ApiInfo");
        activity.GetTagItem("http.route.version").Should().Be("2");

        var innerActivity = capture.GetSingleByDisplayName("GetInfoJwt");
        innerActivity.GetTagItem("operation.type").Should().Be("read");
        innerActivity.GetTagItem("otel.status_code").Should().Be("OK");
    }

    [Fact]
    public async Task FailedAuthentication_TelemetryTags_InnerActivityHasErrorStatusAndExceptionEvent()
    {
        // Arrange
        using var capture = new ActivityCapture();
        var client = _factory.CreateApiKeyClient();
        var request = new { Username = "admin", Password = "wrong-password" };

        // Act
        var response = await client.PostAsJsonWithIdempotencyKeyAsync("/api/v2/auth/authenticate", request, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("auth_invalid_credentials");

        var activity = capture.GetSingleByRoute("Auth/authenticate");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("http.route.controller").Should().Be("Auth");
        activity.GetTagItem("http.route.version").Should().Be("2");

        var innerActivity = capture.GetSingleByDisplayName("AuthenticateUser");
        innerActivity.GetTagItem("operation.type").Should().Be("auth");
        innerActivity.GetTagItem("otel.status_code").Should().Be("ERROR");
        innerActivity.GetTagItem("otel.status_description").Should().NotBeNull();

        var exceptionEvent = innerActivity.Events.Should().ContainSingle(e => e.Name == "exception").Which;
        exceptionEvent.Tags.Should().Contain(t => t.Key == "exception.type" && t.Value!.ToString()!.Contains("UnauthorizedAccessException"));
        exceptionEvent.Tags.Should().Contain(t => t.Key == "exception.message");
        exceptionEvent.Tags.Should().Contain(t => t.Key == "exception.stacktrace");
    }

    [Fact]
    public async Task UnauthenticatedJwtRequest_TelemetryTags_OuterActivityHasErrorStatusAndNoInnerActivity()
    {
        // Arrange
        using var capture = new ActivityCapture();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v2/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("jwt_authentication_required");

        var activity = capture.GetSingleByRoute("ApiInfo/info-jwt");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("enduser.authenticated").Should().Be(false);
        activity.GetTagItem("enduser.name").Should().BeNull();
        activity.GetTagItem("enduser.id").Should().BeNull();
        activity.GetTagItem("http.route.controller").Should().Be("ApiInfo");
        activity.GetTagItem("http.route.version").Should().Be("2");

        capture.GetActivities().Should().NotContain(a => a.DisplayName == "GetInfoJwt",
            "controller action should not execute when authentication fails at middleware level");
    }

    [Fact]
    public async Task JwtWithoutPermission_TelemetryTags_OuterActivityHasErrorStatusAndNoInnerActivity()
    {
        // Arrange — JWT without token.create permission hitting an endpoint that requires it
        using var capture = new ActivityCapture();
        var client = _factory.CreateJwtClient(TestUserId, TestUsername);
        var request = new { UsernameSuffix = "test-token", Permissions = new[] { "api.read" } };

        // Act
        var response = await client.PostAsJsonWithIdempotencyKeyAsync("/api/v2/auth/generate-token", request, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Forbidden);
        await response.Should().HaveErrorCode("forbidden");

        var activity = capture.GetSingleByRoute("Auth/generate-token");
        activity.GetTagItem("enduser.authenticated").Should().NotBeNull("enrichment should always set enduser.authenticated");
        activity.GetTagItem("enduser.authenticated").Should().Be(true);
        activity.GetTagItem("enduser.name").Should().Be(TestUsername);
        activity.GetTagItem("enduser.id").Should().Be(TestUserId.ToString());
        activity.GetTagItem("http.route.controller").Should().Be("Auth");
        activity.GetTagItem("http.route.version").Should().Be("2");

        capture.GetActivities().Should().NotContain(a => a.DisplayName == "GenerateToken",
            "controller action should not execute when authorization fails at middleware level");
    }
}
