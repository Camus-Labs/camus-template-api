using System.Data;
using System.Data.Common;
using FluentAssertions;
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
    private static readonly Guid Jti = Guid.Parse("a1b2c3d4-0001-0002-0003-000000000001");
    private static readonly Guid CreatorUserId = Guid.Parse("b2c3d4e5-0001-0002-0003-000000000002");
    private static readonly string[] TokenPermissions = new[] { "read", "write" };

    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IGeneratedTokenDataAccess> _mockDataAccess = new();
    private readonly Mock<DbConnection> _mockConnection = new();
    private readonly UnitOfWork _unitOfWork;

    public GeneratedTokenRepositoryTests()
    {
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
        return new GeneratedTokenRepository(_unitOfWork, _mockDataAccess.Object);
    }

    private static GeneratedToken CreateToken()
    {
        return GeneratedToken.Reconstitute(
            Jti, CreatorUserId, "creator", "creator-suffix",
            new List<string> { "read", "write" },
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(-1),
            false, null);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        UnitOfWork? unitOfWork = null;

        // Act
        var act = () => new GeneratedTokenRepository(unitOfWork!, _mockDataAccess.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_NullDataAccess_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_mockConnectionFactory.Object);

        // Act
        var act = () => new GeneratedTokenRepository(unitOfWork, null!);

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
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), "creator-suffix", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DataConflictException>()
            .WithMessage("*username*");
    }

    [Fact]
    public async Task CreateAsync_CreatorNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = CreateRepository();
        _mockDataAccess
            .Setup(d => d.JtiExistsAsync(It.IsAny<IDbConnection>(), Jti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), "creator-suffix", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockDataAccess
            .Setup(d => d.CreatorUserExistsAsync(It.IsAny<IDbConnection>(), CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => repository.CreateAsync(CreateToken(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
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
            .Setup(d => d.TokenUsernameExistsAsync(It.IsAny<IDbConnection>(), "creator-suffix", It.IsAny<CancellationToken>()))
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
            .ReturnsAsync((GeneratedTokenModel?)null);

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
                Jti = Jti, CreatorUserId = CreatorUserId, CreatorUsername = "creator",
                TokenUsername = "creator-suffix", Permissions = TokenPermissions,
                ExpiresOn = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            });

        // Act
        var result = await repository.GetByJtiAsync(Jti, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Jti.Should().Be(Jti);
        result.TokenUsername.Should().Be("creator-suffix");
    }

    // --- GetPagedByCreatorUserIdAsync ---

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_EmptyGuid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetPagedByCreatorUserIdAsync(Guid.Empty, new PaginationParams(), ct: TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetPagedByCreatorUserIdAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var act = () => repository.GetPagedByCreatorUserIdAsync(CreatorUserId, null!, ct: TestContext.Current.CancellationToken);

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
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), ct: TestContext.Current.CancellationToken);

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
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockDataAccess
            .Setup(d => d.GetPageByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, false, false, 10, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GeneratedTokenModel
                {
                    Jti = Jti, CreatorUserId = CreatorUserId, CreatorUsername = "creator",
                    TokenUsername = "creator-suffix", Permissions = TokenPermissions,
                    ExpiresOn = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                }
            });

        // Act
        var result = await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, pagination, ct: TestContext.Current.CancellationToken);

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
            .Setup(d => d.CountByCreatorUserIdAsync(It.IsAny<IDbConnection>(), CreatorUserId, true, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await repository.GetPagedByCreatorUserIdAsync(CreatorUserId, new PaginationParams(), filter, TestContext.Current.CancellationToken);

        // Assert
        _mockDataAccess.Verify(d => d.CountByCreatorUserIdAsync(
            It.IsAny<IDbConnection>(), CreatorUserId, true, true, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(d => d.GetActiveRevokedJtisAsync(It.IsAny<IDbConnection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Jti });

        // Act
        var result = await repository.GetActiveRevokedJtisAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(Jti);
    }
}
