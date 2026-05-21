using FluentAssertions;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using Microsoft.Extensions.Time.Testing;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class GeneratedTokenMappingExtensionsTests
{
    private static readonly FakeTimeProvider TimeProvider = new(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ValidJti = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidUserId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTime FixedExpiresOn = TimeProvider.GetUtcNow().AddYears(1).UtcDateTime;
    private static readonly DateTime FixedCreatedAt = TimeProvider.GetUtcNow().UtcDateTime;
    private static readonly string[] ExpectedPermissions = ["read", "write"];

    // --- ToEntity ---

    [Fact]
    public void ToEntity_ValidModel_MapsAllProperties()
    {
        // Arrange
        var model = new GeneratedTokenModel
        {
            Jti = ValidJti,
            CreatorUserId = ValidUserId,
            CreatorUsername = "admin",
            TokenUsername = "admin-service",
            Permissions = new[] { "read", "write" },
            ExpiresOn = FixedExpiresOn,
            CreatedAt = FixedCreatedAt,
            IsRevoked = false,
            RevokedAt = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Jti.Should().Be(ValidJti);
        entity.CreatorUserId.Should().Be(ValidUserId);
        entity.CreatorUsername.Should().Be("admin");
        entity.TokenUsername.Should().Be("admin-service");
        entity.Permissions.Should().BeEquivalentTo(ExpectedPermissions);
        entity.ExpiresOn.Should().Be(FixedExpiresOn);
        entity.CreatedAt.Should().Be(FixedCreatedAt);
        entity.IsRevoked.Should().BeFalse();
        entity.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullPermissions_MapsToEmptyList()
    {
        // Arrange
        var model = new GeneratedTokenModel
        {
            Jti = ValidJti,
            CreatorUserId = ValidUserId,
            CreatorUsername = "admin",
            TokenUsername = "admin-service",
            Permissions = null,
            ExpiresOn = FixedExpiresOn,
            CreatedAt = FixedCreatedAt,
            IsRevoked = false,
            RevokedAt = null
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        entity.Permissions.Should().BeEmpty();
    }
}
