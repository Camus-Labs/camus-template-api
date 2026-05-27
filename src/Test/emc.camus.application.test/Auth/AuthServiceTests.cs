using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.application.Observability;
using emc.camus.domain.Auth;
using emc.camus.domain.Exceptions;

namespace emc.camus.application.test.Auth;

public class AuthServiceTests
{
    private static readonly Guid ValidUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string ValidUsername = "admin";
    private const string ValidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.test-token";
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidExpiration = FixedNow.UtcDateTime.AddYears(1).AddDays(-1);
    private static readonly DateTime ValidTokenExpiration = FixedNow.UtcDateTime.AddMonths(6);
    private static readonly DateTime ValidCreatedAt = FixedNow.UtcDateTime.AddYears(-1);
    private static readonly Guid ValidJti = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidTokenSuffix = "token1";
    private const string ValidPassword = "password123";
    private const string ValidTokenFullUsername = "admin-token1";
    private const string DbConnectionFailedMessage = "DB connection failed";
    private const string DbWriteFailedMessage = "DB write failed";
    private const string UserNotFoundMessage = "User not found";
    private const long AuditRecordId = 1L;
    private static readonly IReadOnlyList<string> ValidTokenPermissions = [Permissions.ApiRead];
    private static readonly IReadOnlyList<string> AdminPermissions = [Permissions.ApiRead, Permissions.ApiWrite, Permissions.TokenCreate];

    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IActionAuditRepository> _auditRepositoryMock;
    private readonly Mock<ITokenRevocationCache> _tokenRevocationCacheMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IActivitySourceWrapper> _activitySourceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGeneratedTokenRepository> _generatedTokenRepositoryMock;
    private readonly FakeTimeProvider _fakeTimeProvider;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _auditRepositoryMock = new Mock<IActionAuditRepository>();
        _tokenRevocationCacheMock = new Mock<ITokenRevocationCache>();
        _userContextMock = new Mock<IUserContext>();
        _activitySourceMock = new Mock<IActivitySourceWrapper>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _generatedTokenRepositoryMock = new Mock<IGeneratedTokenRepository>();
        _fakeTimeProvider = new FakeTimeProvider(FixedNow);
    }

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
        var role = new Role("admin", permissions: AdminPermissions.ToList());
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

    [Fact]
    public void Constructor_NullUserRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            null!, _tokenGeneratorMock.Object, _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object, _userContextMock.Object,
            _activitySourceMock.Object, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userRepository");
    }

    [Fact]
    public void Constructor_NullTokenGenerator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, null!, _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object, _userContextMock.Object,
            _activitySourceMock.Object, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tokenGenerator");
    }

    [Fact]
    public void Constructor_NullAuditRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, _tokenGeneratorMock.Object, null!,
            _tokenRevocationCacheMock.Object, _userContextMock.Object,
            _activitySourceMock.Object, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("auditRepository");
    }

    [Fact]
    public void Constructor_NullTokenRevocationCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, _tokenGeneratorMock.Object, _auditRepositoryMock.Object,
            null!, _userContextMock.Object,
            _activitySourceMock.Object, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tokenRevocationCache");
    }

    [Fact]
    public void Constructor_NullUserContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, _tokenGeneratorMock.Object, _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object, null!,
            _activitySourceMock.Object, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userContext");
    }

    [Fact]
    public void Constructor_NullActivitySource_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, _tokenGeneratorMock.Object, _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object, _userContextMock.Object,
            null!, _unitOfWorkMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("activitySource");
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AuthService(
            _userRepositoryMock.Object, _tokenGeneratorMock.Object, _auditRepositoryMock.Object,
            _tokenRevocationCacheMock.Object, _userContextMock.Object,
            _activitySourceMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    // --- AuthenticateAsync ---

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsResult()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, ValidPassword);
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, ValidPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(user.Id, user.Username, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _auditRepositoryMock.Setup(a => a.LogActionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditRecordId);

        var service = CreateService();

        // Act
        var result = await service.AuthenticateAsync(command, TestContext.Current.CancellationToken);

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
        var act = () => service.AuthenticateAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, ValidPassword);
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, ValidPassword, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, ValidPassword);
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, ValidPassword, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(UserNotFoundMessage));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AuthenticateAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, ValidPassword);
        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, ValidPassword, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbConnectionFailedMessage));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Authentication failed*system error*");
    }

    [Fact]
    public async Task AuthenticateAsync_InfrastructureFailure_RollsBackTransaction()
    {
        // Arrange
        var command = new AuthenticateUserCommand(ValidUsername, ValidPassword);
        var user = CreateUser();

        _userRepositoryMock.Setup(r => r.ValidateCredentialsAsync(ValidUsername, ValidPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbWriteFailedMessage));

        var service = CreateService();

        // Act
        var act = () => service.AuthenticateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // --- GenerateTokenAsync ---

    [Fact]
    public async Task GenerateTokenAsync_ValidCommand_ReturnsResult()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), ValidTokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);

        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.Token.Should().Be(ValidToken);
        result.ExpiresOn.Should().Be(ValidExpiration);
        result.TokenUsername.Should().Contain(ValidTokenSuffix);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithRepository_PersistsToken()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), ValidTokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditRecordId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        await service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

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
        var act = () => service.GenerateTokenAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task GenerateTokenAsync_NoUserContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task GenerateTokenAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbConnectionFailedMessage));

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token generation failed*system error*");
    }

    [Fact]
    public async Task GenerateTokenAsync_RepositoryCreateFails_RollsBackTransaction()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), ValidTokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _generatedTokenRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<GeneratedToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbWriteFailedMessage));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(UserNotFoundMessage));

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert — original message preserved, not wrapped as system error
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*User not found*");
    }

    public static readonly TheoryData<Exception> GenerateTokenAsync_BusinessExceptionCases = new()
    {
        new ArgumentException("Invalid token parameters"),
        new DomainException("Domain rule violated")
    };

    [Theory]
    [MemberData(nameof(GenerateTokenAsync_BusinessExceptionCases))]
    public async Task GenerateTokenAsync_BusinessException_RethrowsWithOriginalMessage(Exception expectedException)
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        var user = CreateUser();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<IEnumerable<Claim>>()))
            .Throws(expectedException);

        var service = CreateService();

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert — original message preserved, not wrapped as system error
        (await act.Should().ThrowAsync<Exception>())
            .Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task GenerateTokenAsync_DuplicateToken_ThrowsDataConflictException()
    {
        // Arrange
        var command = new GenerateTokenCommand(ValidTokenSuffix, ValidTokenExpiration, ValidTokenPermissions.ToList());
        var user = CreateUser();
        var authToken = CreateAuthToken();

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGeneratorMock.Setup(g => g.GenerateToken(
                ValidUserId, It.IsAny<string>(), It.IsAny<Guid>(), ValidTokenExpiration, It.IsAny<IEnumerable<Claim>>()))
            .Returns(authToken);
        _generatedTokenRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<GeneratedToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DataConflictException("Duplicate token"));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GenerateTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert — original message preserved, not wrapped as system error
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*Duplicate token*");
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
            ValidJti, ValidUserId, ValidUsername, ValidTokenFullUsername,
            ValidTokenPermissions.ToList(), ValidExpiration,
            ValidCreatedAt, false, null, _fakeTimeProvider);
        var pagedTokens = new PagedResult<GeneratedToken>([token], 1, 1, 25);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetPagedByCreatorUserIdAsync(ValidUserId, pagination, filter, It.IsAny<SortParams<GeneratedTokenSortField>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedTokens);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var result = await service.GetGeneratedTokensAsync(pagination, filter, new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

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
        var act = () => service.GetGeneratedTokensAsync(null!, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("pagination");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_NoUserContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var pagination = new PaginationParams();
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_NoRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var pagination = new PaginationParams();
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);

        var service = CreateService(generatedTokenRepository: null);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*token repository*");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var pagination = new PaginationParams();
        var filter = new GeneratedTokenFilter();
        var sort = new SortParams<GeneratedTokenSortField>();
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetPagedByCreatorUserIdAsync(ValidUserId, pagination, It.IsAny<GeneratedTokenFilter>(), It.IsAny<SortParams<GeneratedTokenSortField>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbConnectionFailedMessage));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to retrieve generated tokens*system error*");
    }

    [Fact]
    public async Task GetGeneratedTokensAsync_ArgumentException_RethrowsWithOriginalMessage()
    {
        // Arrange
        var pagination = new PaginationParams();
        var filter = new GeneratedTokenFilter();
        var sort = new SortParams<GeneratedTokenSortField>();
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetPagedByCreatorUserIdAsync(ValidUserId, pagination, It.IsAny<GeneratedTokenFilter>(), It.IsAny<SortParams<GeneratedTokenSortField>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid filter value"));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.GetGeneratedTokensAsync(pagination, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert — original message preserved, not wrapped as system error
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid filter value*");
    }

    // --- RevokeTokenAsync ---

    [Fact]
    public async Task RevokeTokenAsync_ValidCommand_ReturnsRevokedView()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        var token = GeneratedToken.Reconstitute(
            ValidJti, ValidUserId, ValidUsername, ValidTokenFullUsername,
            ValidTokenPermissions.ToList(), ValidExpiration,
            ValidCreatedAt, false, null, _fakeTimeProvider);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditRecordId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var result = await service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.Jti.Should().Be(ValidJti);
        result.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().Be(_fakeTimeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task RevokeTokenAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task RevokeTokenAsync_NoUserContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        Guid? noUserId = null;
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(noUserId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*User ID*not available*");
    }

    [Fact]
    public async Task RevokeTokenAsync_NoRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);

        var service = CreateService(generatedTokenRepository: null);

        // Act
        var act = () => service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*token repository*");
    }

    [Fact]
    public async Task RevokeTokenAsync_TokenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        GeneratedToken? noToken = null;
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(noToken);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

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
            ValidTokenPermissions.ToList(), ValidExpiration,
            ValidCreatedAt, false, null, _fakeTimeProvider);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

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
            ValidJti, ValidUserId, ValidUsername, ValidTokenFullUsername,
            ValidTokenPermissions.ToList(), ValidExpiration,
            ValidCreatedAt, false, null, _fakeTimeProvider);

        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _auditRepositoryMock.Setup(a => a.LogCurrentUserActionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditRecordId);

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        await service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

        // Assert
        _tokenRevocationCacheMock.Verify(c => c.Revoke(ValidJti), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_InfrastructureFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new RevokeTokenCommand(ValidJti);
        _userContextMock.Setup(c => c.GetCurrentUserId()).Returns(ValidUserId);
        _generatedTokenRepositoryMock.Setup(r => r.GetByJtiAsync(ValidJti, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(DbConnectionFailedMessage));

        var service = CreateService(_generatedTokenRepositoryMock.Object);

        // Act
        var act = () => service.RevokeTokenAsync(command, TestContext.Current.CancellationToken);

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
        await service.InitializeAsync(TestContext.Current.CancellationToken);

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
        var act = () => service.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to initialize*authentication*");
    }

}
