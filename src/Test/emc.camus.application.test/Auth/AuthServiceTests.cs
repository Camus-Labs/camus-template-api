using System.Security.Claims;
using FluentAssertions;
using Moq;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Observability;
using emc.camus.domain.Auth;

namespace emc.camus.application.test.Auth;

public class AuthServiceTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidUsername = "admin";
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTime ValidExpiration = new(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidTokenSuffix = "token1";

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IActionAuditRepository> _auditRepositoryMock = new();
    private readonly Mock<ITokenRevocationCache> _tokenRevocationCacheMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IActivitySourceWrapper> _activitySourceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGeneratedTokenRepository> _generatedTokenRepositoryMock = new();

    private AuthService CreateService(IGeneratedTokenRepository? generatedTokenRepository = null)
    {
        return new AuthService(
            _userRepositoryMock.Object,
            _tokenGeneratorMock.Object,
            _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object,
            _userContextMock.Object,
            _activitySourceMock.Object,
            _unitOfWorkMock.Object,
            generatedTokenRepository);
    }

    private static User CreateUser(Guid? id = null, string username = ValidUsername)
    {
        var role = new Role("admin", permissions: [Permissions.ApiRead, Permissions.ApiWrite, Permissions.TokenCreate]);
        return new User(username, [role], id ?? ValidUserId);
    }

    private static AuthToken CreateAuthToken()
    {
        return new AuthToken(ValidToken, ValidExpiration);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, "userRepository")]
    [InlineData(1, "tokenGenerator")]
    [InlineData(2, "auditRepository")]
    [InlineData(3, "tokenRevocationCache")]
    [InlineData(4, "userContext")]
    [InlineData(5, "activitySource")]
    [InlineData(6, "unitOfWork")]
    public void Constructor_NullDependency_ThrowsArgumentNullException(int nullIndex, string expectedParamName)
    {
        // Arrange
        var deps = new object?[]
        {
            _userRepositoryMock.Object,
            _tokenGeneratorMock.Object,
            _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object,
            _userContextMock.Object,
            _activitySourceMock.Object,
            _unitOfWorkMock.Object
        };
        deps[nullIndex] = null;

        // Act
        var act = () => new AuthService(
            (IUserRepository)deps[0]!,
            (ITokenGenerator)deps[1]!,
            (IActionAuditRepository)deps[2]!,
            (ITokenRevocationCache)deps[3]!,
            (IUserContext)deps[4]!,
            (IActivitySourceWrapper)deps[5]!,
            (IUnitOfWork)deps[6]!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be(expectedParamName);
    }

    // --- AuthenticateAsync ---

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsResult()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, "password123");
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(user.Id, user.Username, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _auditRepositoryMock.Setup(a => a.LogActionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService();

        // Act
        var result = await service.AuthenticateAsync(command);

        // Assert
        result.Token.Should().Be(ValidToken);
        result.ExpiresOn.Should().Be(ValidExpiration);
    }

    [Fact]
    public async Task AuthenticateAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, "password123");
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, "password123", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, "password123");
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, "password123", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AuthenticateAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, "password123");
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, "password123", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Authentication failed*system error*");
    }

    [Fact]
    public async Task AuthenticateAsync_InfrastructureFailure_RollsBackTransaction()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, "password123");
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // --- GenerateTokenAsync ---

    [Fact]
    public async Task GenerateTokenAsync_ValidCommand_ReturnsResult()
    {
        // Arrange
        var tokenExpiration = DateTime.UtcNow.AddMonths(6);
        var command = new GenerateTokenCommand(ValidTokenSuffix, tokenExpiration, [Permissions.ApiRead]);
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), tokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);

        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync(command);

        // Assert
        result.Token.Should().Be(ValidToken);
        result.ExpiresOn.Should().Be(ValidExpiration);
        result.TokenUsername.Should().Contain(ValidTokenSuffix);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithRepository_PersistsToken()
    {
        // Arrange
        var tokenExpiration = DateTime.UtcNow.AddMonths(6);
        var command = new GenerateTokenCommand(ValidTokenSuffix, tokenExpiration, [Permissions.ApiRead]);
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), tokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        await service.GenerateTokenAsync(command);

        // Assert
        _generatedTokenRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<GeneratedToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task GenerateTokenAsync_NoUserContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, DateTime.UtcNow.AddMonths(6), [Permissions.ApiRead]);
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task GenerateTokenAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, DateTime.UtcNow.AddMonths(6), [Permissions.ApiRead]);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token generation failed*system error*");
    }

    [Fact]
    public async Task GenerateTokenAsync_RepositoryCreateFails_RollsBackTransaction()
    {
        // Arrange
        var tokenExpiration = DateTime.UtcNow.AddMonths(6);
        var command = new GenerateTokenCommand(ValidTokenSuffix, tokenExpiration, [Permissions.ApiRead]);
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), tokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _generatedTokenRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<GeneratedToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GenerateTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // --- GetGeneratedTokensAsync ---

    [Fact]
    public async Task GetGeneratedTokensAsync_ValidRequest_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParams(1, 25);
        var filter = new GeneratedTokenFilter();
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidUserId, ValidUsername, "admin-token1",
            [Permissions.ApiRead], ValidExpiration,
            new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), false, null);
        var pagedTokens = new PagedResult<GeneratedToken>([token], 1, 1, 25);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetPagedByCreatorUserIdAsync(ValidUserId, pagination, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedTokens);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var result = await service.GetGeneratedTokensAsync(pagination, filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].Jti.Should().Be(ValidJti);
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("pagination");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_NoUserContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var pagination = new PaginationParams();
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_NoRepository_ThrowsInvalidOperationException()
    {
        // Arrange
        var pagination = new PaginationParams();
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);

        var service = CreateService(generatedTokenRepository: null);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*token repository*");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var pagination = new PaginationParams();
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetPagedByCreatorUserIdAsync(ValidUserId, pagination, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to retrieve generated tokens*system error*");
    }

    // --- RevokeTokenAsync ---

    [Fact]
    public async Task RevokeTokenAsync_ValidCommand_ReturnsRevokedView()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidUserId, ValidUsername, "admin-token1",
            [Permissions.ApiRead], ValidExpiration,
            new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), false, null);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var result = await service.RevokeTokenAsync(command);

        // Assert
        result.Jti.Should().Be(ValidJti);
        result.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task RevokeTokenAsync_NoUserContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task RevokeTokenAsync_NoRepository_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);

        var service = CreateService(generatedTokenRepository: null);

        // Act
        var act = () => service.RevokeTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*token repository*");
    }

    [Fact]
    public async Task RevokeTokenAsync_TokenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        GeneratedToken? noToken = null;
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(noToken);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RevokeTokenAsync_NotCreator_ThrowsUnauthorizedViaRollback()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        var differentUserId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var token = GeneratedToken.Reconstitute(
            ValidJti, differentUserId, "otheruser", "otheruser-token1",
            [Permissions.ApiRead], ValidExpiration,
            new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), false, null);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_CacheRevokeCalled_OnSuccess()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidUserId, ValidUsername, "admin-token1",
            [Permissions.ApiRead], ValidExpiration,
            new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), false, null);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        await service.RevokeTokenAsync(command);

        // Assert
        _tokenRevocationCacheMock.Verify(c => c.Revoke(ValidJti), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userContextMock.Setup(c => c.GetCurrentUsername()).Returns(ValidUsername);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token revocation failed*system error*");
    }

    // --- InitializeAsync ---

    [Fact]
    public async Task InitializeAsync_Success_CallsRepositoryInitializeAsync()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        _userRepositoryMock.Verify(r => r.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_RepositoryFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.InitializeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Connection failed"));
        var service = CreateService();

        // Act
        var act = () => service.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to initialize*authentication*");
    }

}
