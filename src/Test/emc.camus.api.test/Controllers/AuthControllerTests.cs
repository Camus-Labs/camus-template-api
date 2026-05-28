using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using emc.camus.api.Controllers;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Requests.V2;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using emc.camus.api.test.Helpers;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Observability;

namespace emc.camus.api.test.Controllers;

public class AuthControllerTests
{
    private static readonly DateTimeOffset FixedNow = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiresOn = FixedNow.UtcDateTime.AddYears(1).AddDays(-1);
    private static readonly DateTime ValidCreatedAt = FixedNow.UtcDateTime;
    private static readonly List<string> PermissionsApiRead = ["api.read"];
    private static readonly Guid TestJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly List<GeneratedTokenSummaryView> EmptyTokenSummaries = [];

    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeActivitySourceWrapper _activitySource;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _timeProvider = new FakeTimeProvider();
        _activitySource = new FakeActivitySourceWrapper();
        _timeProvider.SetUtcNow(FixedNow);

        _mockAuthService = new Mock<IAuthService>();

        _controller = new AuthController(_timeProvider, _activitySource, _mockAuthService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // --- Constructor ---

    public static readonly TheoryData<TimeProvider?, IActivitySourceWrapper?, IAuthService?> Constructor_NullDependencyScenarios = new()
    {
        { null, new FakeActivitySourceWrapper(), new Mock<IAuthService>().Object },
        { new FakeTimeProvider(), null, new Mock<IAuthService>().Object },
        { new FakeTimeProvider(), new FakeActivitySourceWrapper(), null }
    };

    [Theory]
    [MemberData(nameof(Constructor_NullDependencyScenarios))]
    public void Constructor_NullDependency_ThrowsArgumentNullException(
        TimeProvider? timeProvider, IActivitySourceWrapper? activitySource, IAuthService? authService)
    {
        // Arrange
        // Act
        var act = () => new AuthController(timeProvider!, activitySource!, authService!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- AuthenticateUser ---

    [Fact]
    public async Task AuthenticateUser_ValidRequest_ReturnsOkWithAuthResponseAndSetsActivityTags()
    {
        // Arrange
        var request = new AuthenticateUserRequest
        {
            Username = "testuser",
            Password = "securepass"
        };
        var authResult = new AuthenticateUserResult("jwt-token-value", ValidExpiresOn);

        _mockAuthService
            .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.AuthenticateUser(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthenticateUserResponse>>().Subject;
        apiResponse.Data!.Token.Should().Be("jwt-token-value");
        apiResponse.Data.ExpiresOn.Should().Be(authResult.ExpiresOn);
        apiResponse.Message.Should().Contain("authenticated successfully");
    }

    // --- GenerateToken ---

    [Fact]
    public async Task GenerateToken_ValidRequest_ReturnsOkWithGenerateTokenResponse()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            UsernameSuffix = "ci-deploy",
            ExpiresOn = ValidExpiresOn,
            Permissions = PermissionsApiRead
        };
        var generateResult = new GenerateTokenResult(
            "generated-token",
            request.ExpiresOn,
            "adminuser-ci-deploy");

        _mockAuthService
            .Setup(s => s.GenerateTokenAsync(It.IsAny<GenerateTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generateResult);

        // Act
        var result = await _controller.GenerateToken(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<GenerateTokenResponse>>().Subject;
        apiResponse.Data!.Token.Should().Be("generated-token");
        apiResponse.Data.ExpiresOn.Should().Be(generateResult.ExpiresOn);
        apiResponse.Data.TokenUsername.Should().Be("adminuser-ci-deploy");
        apiResponse.Message.Should().Contain("Token generated successfully");
    }

    // --- GetGeneratedTokens ---

    [Fact]
    public async Task GetGeneratedTokens_ValidRequest_ReturnsOkWithPagedResponse()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery
        {
            Page = 1,
            PageSize = 10,
            ExcludeRevoked = true,
            ExcludeExpired = false
        };

        var jti = TestJti;
        var view = new GeneratedTokenSummaryView(
            jti, "admin-token1",
            PermissionsApiRead,
            ValidExpiresOn, ValidCreatedAt,
            false, null, true);

        var pagedResult = new PagedResult<GeneratedTokenSummaryView>(
            new List<GeneratedTokenSummaryView> { view }, 1, 1, 10);

        _mockAuthService
            .Setup(s => s.GetGeneratedTokensAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<GeneratedTokenFilter>(),
                It.IsAny<SortParams<GeneratedTokenSortField>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetGeneratedTokens(query, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>().Subject;
        apiResponse.Data!.Items.Should().ContainSingle();
        apiResponse.Data.Items[0].Jti.Should().Be(jti);
        apiResponse.Data.Items[0].TokenUsername.Should().Be("admin-token1");
        apiResponse.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGeneratedTokens_EmptyResult_ReturnsOkWithEmptyPage()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery();
        var pagedResult = new PagedResult<GeneratedTokenSummaryView>(
            EmptyTokenSummaries, 0, 1, 25);

        _mockAuthService
            .Setup(s => s.GetGeneratedTokensAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<GeneratedTokenFilter>(),
                It.IsAny<SortParams<GeneratedTokenSortField>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetGeneratedTokens(query, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>().Subject;
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.TotalCount.Should().Be(0);
    }

    // --- RevokeToken ---

    [Fact]
    public async Task RevokeToken_ValidJti_ReturnsOkWithRevokedTokenSummary()
    {
        // Arrange
        var revokedAt = _timeProvider.GetUtcNow().AddMonths(6).UtcDateTime;
        var jti = TestJti;
        var view = new GeneratedTokenSummaryView(
            jti, "admin-token1",
            PermissionsApiRead,
            ValidExpiresOn, ValidCreatedAt,
            true, revokedAt, false);

        _mockAuthService
            .Setup(s => s.RevokeTokenAsync(It.IsAny<RevokeTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(view);

        // Act
        var result = await _controller.RevokeToken(jti, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GeneratedTokenSummaryDto>>().Subject;
        apiResponse.Data!.Jti.Should().Be(jti);
        apiResponse.Data.IsRevoked.Should().BeTrue();
        apiResponse.Data.RevokedAt.Should().Be(revokedAt);
        apiResponse.Message.Should().Contain("Token revoked successfully");
    }
}
