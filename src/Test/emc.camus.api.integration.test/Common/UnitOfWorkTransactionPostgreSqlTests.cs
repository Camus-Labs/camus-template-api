using Dapper;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class UnitOfWorkTransactionPostgreSqlTests : IAsyncLifetime
{
    private readonly ApiPostgreSqlFactory _factory;

    public UnitOfWorkTransactionPostgreSqlTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    public async ValueTask InitializeAsync() => await _factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Transaction_UncommittedWrite_NotVisibleOutsideTransactionScope()
    {
        // Arrange — resolve real scoped services from the DI container
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tokenRepository = scope.ServiceProvider.GetRequiredService<IGeneratedTokenRepository>();

        var jti = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var creatorUserId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"); // Admin from seed
        var token = GeneratedToken.Reconstitute(
            jti, creatorUserId, "Admin", "Admin-rollback-test",
            ["api.read"], DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow, false, null);

        // Act — begin transaction and insert a token
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await tokenRepository.CreateAsync(token, TestContext.Current.CancellationToken);

        // Assert — row is visible within the transaction via the same repository
        var duringTransaction = await tokenRepository.GetByJtiAsync(jti, TestContext.Current.CancellationToken);
        duringTransaction!.TokenUsername.Should().Be("Admin-rollback-test", "inserted row must be readable within the transaction");

        // Assert — row is NOT visible from an outside connection (transaction isolation)
        await using var outsideConnection = new NpgsqlConnection(_factory.ConnectionString);
        await outsideConnection.OpenAsync(TestContext.Current.CancellationToken);
        var countOutside = await outsideConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });
        countOutside.Should().Be(0, "uncommitted row should not be visible outside the transaction");

        // Act — rollback the transaction
        await unitOfWork.RollbackAsync();

        // Assert — row is gone after rollback
        var afterRollback = await tokenRepository.GetByJtiAsync(jti, TestContext.Current.CancellationToken);
        afterRollback.Should().BeNull("rolled-back row must not be persisted");
    }

    [Fact]
    public async Task Transaction_CommitAfterInsert_PersistsTokenToDatabase()
    {
        // Arrange — resolve real scoped services from the DI container
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tokenRepository = scope.ServiceProvider.GetRequiredService<IGeneratedTokenRepository>();

        var jti = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var creatorUserId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"); // Admin from seed
        var token = GeneratedToken.Reconstitute(
            jti, creatorUserId, "Admin", "Admin-commit-test",
            ["api.read"], DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow, false, null);

        // Act — begin transaction, insert a token
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await tokenRepository.CreateAsync(token, TestContext.Current.CancellationToken);

        // Assert — row is NOT visible from an outside connection before commit
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var countBeforeCommit = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });
        countBeforeCommit.Should().Be(0, "uncommitted row should not be visible outside the transaction");

        // Act — commit
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert — row is visible from the outside connection after commit
        var countAfterCommit = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });
        countAfterCommit.Should().Be(1, "committed row must be visible outside the transaction");
    }
}
