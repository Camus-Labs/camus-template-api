using System.Data;
using System.Data.Common;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Repositories;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.test.Repositories;

public class GeneratedTokenRepositoryTests : IDisposable
{
    private const string CreatorUsername = "creator";
    private const string TokenUsername = "creator-suffix";
    private static readonly DateTimeOffset ReferenceTime = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Jti = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");
    private static readonly Guid[] SingleJtiArray = [Jti];
    private static readonly Guid CreatorUserId = Guid.Parse("b2c3d4e5-0001-0002-0003-000000000002");
    private static readonly string[] TokenPermissions = new[] { "read", "write" };
    private static readonly DateTime FixedExpiresOn = ReferenceTime.AddYears(1).UtcDateTime;
    private static readonly DateTime FixedCreatedAt = ReferenceTime.UtcDateTime;
    private static readonly GeneratedTokenModel[] SingleTokenPage = new[]
    {
        new GeneratedTokenModel
        {
            Jti = Jti, CreatorUserId = CreatorUserId, CreatorUsername = CreatorUsername,
            TokenUsername = TokenUsername, Permissions = TokenPermissions,
            ExpiresOn = FixedExpiresOn, CreatedAt = FixedCreatedAt,
            IsRevoked = false
        }
    };

    private readonly Mock<IConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IGeneratedTokenDataAccess> _mockDataAccess;
    private readonly Mock<DbConnection> _mockConnection;
    private readonly FakeTimeProvider _timeProvider;
    private readonly UnitOfWork _unitOfWork;

    public GeneratedTokenRepositoryTests()
    {
        _mockConnectionFactory = new Mock<IConnectionFactory>();
        _mockDataAccess = new Mock<IGeneratedTokenDataAccess>();
        _mockConnection = new Mock<DbConnection>();
        _timeProvider = new FakeTimeProvider(ReferenceTime);

        _mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockConnection.Object);

        _unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        GC.SuppressFinalize(this);
    }

    private GeneratedTokenRepository CreateRepository()
    {
        return new GeneratedTokenRepository(_unitOfWork, _mockDataAccess.Object, _timeProvider);
    }

    private static GeneratedToken CreateToken()
    {
        return GeneratedToken.Reconstitute(
            Jti, CreatorUserId, CreatorUsername, TokenUsername,
            new List<string>(TokenPermissions),
            FixedExpiresOn, FixedCreatedAt,
            false, null);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GeneratedTokenRepository(null!, _mockDataAccess.Object, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GeneratedTokenRepository(_unitOfWork, null!, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("dataAccess");
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_NullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.CreateAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("generatedToken");
    }

    [Fact]
    public async Task CreateAsync_DuplicateJti_ThrowsDataConflictException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.JtiExistsAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*JTI*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateTokenUsername_ThrowsDataConflictException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.JtiExistsAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), TokenUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*username*");
    }

    [Fact]
    public async Task CreateAsync_CreatorNotFound_ThrowsDataConflictException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.JtiExistsAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), TokenUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.CreatorUserExistsAsync(It.IsAny<IDbConnection>(), CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage($"*{CreatorUserId}*");
    }

    [Fact]
    public async Task CreateAsync_ValidToken_CompletesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.JtiExistsAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), TokenUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.CreatorUserExistsAsync(It.IsAny<IDbConnection>(), CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- GetByJtiAsync ---

    [Fact]
    public async Task GetByJtiAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetByJtiAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetByJtiAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.FindByJtiAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(GeneratedTokenModel?));

        // Act
        var result = await repository.GetByJtiAsync(Jti, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByJtiAsync_Found_ReturnsToken()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.FindByJtiAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedTokenModel
            {
                Jti = Jti, CreatorUserId = CreatorUserId, CreatorUsername = CreatorUsername,
                TokenUsername = TokenUsername, Permissions = TokenPermissions,
                ExpiresOn = FixedExpiresOn, CreatedAt = FixedCreatedAt,
                IsRevoked = false
            });

        // Act
        var result = await repository.GetByJtiAsync(Jti, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Jti.Should().Be(Jti);
        result.TokenUsername.Should().Be(TokenUsername);
    }

    // --- GetPagedByCreatorUserIdAsync ---

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetPagedByCreatorUserIdAsync(Guid.Empty, new PaginationParams(), new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetPagedByCreatorUserIdAsync(CreatorUserId, null!, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("pagination");
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_ZeroCount_ReturnsEmptyResult()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_WithResults_ReturnsPagedResult()
    {
        // Arrange
        var repository = CreateRepository();
        var pagination = new PaginationParams(1, 10);
        _mockDataAccess
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockDataAccess
            .Setup(d => d.GetPageByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, 10, 0, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SingleTokenPage);

        // Act
        var result = await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, pagination, new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle()
            .Which.Jti.Should().Be(Jti);
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_WithFilter_PassesFilterToDataAccess()
    {
        // Arrange
        var repository = CreateRepository();
        var filter = new GeneratedTokenFilter(excludeRevoked: true, excludeExpired: true);
        _mockDataAccess
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, true, true, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), filter, new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        _mockDataAccess.Verify(d => d.CountByCreatorUserIdAsync(
            It.IsAny<IDbConnection>(), CreatorUserId, true, true, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("createdAt", "desc", "created_at", "DESC")]
    [InlineData("expiresOn", "asc", "expires_on", "ASC")]
    [InlineData("tokenUsername", "asc", "token_username", "ASC")]
    [InlineData("revokedAt", "desc", "revoked_at", "DESC")]
    public async Task GetPagedByCreatorUserIdAsync_WithSort_PassesMappedColumnAndDirectionToDataAccess(
        string sortBy, string sortDirection, string expectedColumn, string expectedDirection)
    {
        // Arrange
        var repository = CreateRepository();
        var sort = new SortParams<GeneratedTokenSortField>(sortBy, sortDirection);
        _mockDataAccess
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockDataAccess
            .Setup(d => d.GetPageByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, 25, 0, expectedColumn, expectedDirection, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SingleTokenPage);

        // Act
        await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), new GeneratedTokenFilter(), sort: sort, ct: TestContext.Current.CancellationToken);

        // Assert
        _mockDataAccess.Verify(d => d.GetPageByCreatorUserIdAsync(
            It.IsAny<IDbConnection>(), CreatorUserId, false, false, 25, 0, expectedColumn, expectedDirection, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_WithoutSort_PassesNullSortToDataAccess()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockDataAccess
            .Setup(d => d.GetPageByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, 25, 0, null, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SingleTokenPage);

        // Act
        await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), new GeneratedTokenFilter(), new SortParams<GeneratedTokenSortField>(), ct: TestContext.Current.CancellationToken);

        // Assert
        _mockDataAccess.Verify(d => d.GetPageByCreatorUserIdAsync(
            It.IsAny<IDbConnection>(), CreatorUserId, false, false, 25, 0, null, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_NullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.SaveAsync(null!, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("generatedToken");
    }

    [Fact]
    public async Task SaveAsync_TokenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.UpdateRevocationAsync(It.IsAny<IDbConnection>(), Jti, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var act = () => repository.SaveAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{Jti}*");
    }

    [Fact]
    public async Task SaveAsync_TokenExists_CompletesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.UpdateRevocationAsync(It.IsAny<IDbConnection>(), Jti, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var act = () => repository.SaveAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- GetActiveRevokedJtisAsync ---

    [Fact]
    public async Task GetActiveRevokedJtisAsync_ReturnsHashSet()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.GetActiveRevokedJtisAsync(It.IsAny<IDbConnection>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SingleJtiArray);

        // Act
        var result = await repository.GetActiveRevokedJtisAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(Jti);
    }
}
