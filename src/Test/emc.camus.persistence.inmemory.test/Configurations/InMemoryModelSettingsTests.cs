using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;
using emc.camus.persistence.inmemory.test.Helpers;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class InMemoryModelSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate Roles ---

    [Theory]
    [MemberData(nameof(NullOrEmptyRolesData))]
    public void Validate_NullOrEmptyRoles_ThrowsInvalidOperationException(object? roles)
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Roles = (roles as List<RoleSettings>)!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one role*must be defined*");
    }

    [Fact]
    public void Validate_DuplicateRoleName_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Roles.Add(new RoleSettings
        {
            Name = InMemoryModelSettingsFactory.DefaultRoleName,
            Permissions = new List<string> { Permissions.ApiWrite }
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Duplicate role name*{InMemoryModelSettingsFactory.DefaultRoleName}*");
    }

    [Fact]
    public void Validate_InvalidRoleSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Roles = new List<RoleSettings>
        {
            new RoleSettings { Name = "", Permissions = new List<string> { Permissions.ApiRead } }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // --- Validate Users ---

    [Theory]
    [MemberData(nameof(NullOrEmptyUsersData))]
    public void Validate_NullOrEmptyUsers_ThrowsInvalidOperationException(object? users)
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Users = (users as List<UserSettings>)!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one user*must be defined*");
    }

    [Fact]
    public void Validate_DuplicateUsernameSecretName_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Users.Add(new UserSettings
        {
            UsernameSecretName = InMemoryModelSettingsFactory.DefaultUsernameSecret,
            PasswordSecretName = "admin-password-2",
            Roles = new List<string> { InMemoryModelSettingsFactory.DefaultRoleName }
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Duplicate UsernameSecretName*{InMemoryModelSettingsFactory.DefaultUsernameSecret}*");
    }

    [Fact]
    public void Validate_UserWithInvalidRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.Users = new List<UserSettings>
        {
            new UserSettings
            {
                UsernameSecretName = "user-secret",
                PasswordSecretName = "pass-secret",
                Roles = new List<string> { "nonexistent" }
            }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid roles*nonexistent*");
    }

    // --- Validate ApiInfos ---

    [Fact]
    public void Validate_NullApiInfos_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.ApiInfos = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiInfos*cannot be null*");
    }

    [Fact]
    public void Validate_EmptyApiInfos_DoesNotThrow()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.ApiInfos = new List<ApiInfoSettings>();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_DuplicateApiVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.ApiInfos.Add(new ApiInfoSettings
        {
            Name = "Other API",
            Version = InMemoryModelSettingsFactory.DefaultApiVersion,
            Status = "Deprecated"
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Duplicate API version*{InMemoryModelSettingsFactory.DefaultApiVersion}*");
    }

    [Fact]
    public void Validate_InvalidApiInfoSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.ApiInfos = new List<ApiInfoSettings>
        {
            new ApiInfoSettings { Name = "", Version = "1.0", Status = "Available" }
        };

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_MultipleUniqueApiVersions_DoesNotThrow()
    {
        // Arrange
        var settings = InMemoryModelSettingsFactory.Create();
        settings.ApiInfos.Add(new ApiInfoSettings
        {
            Name = "API v2",
            Version = "2.0",
            Status = "Beta"
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    public static IEnumerable<object?[]> NullOrEmptyRolesData() => new List<object?[]>
    {
        new object?[] { null },
        new object?[] { new List<RoleSettings>() }
    };

    public static IEnumerable<object?[]> NullOrEmptyUsersData() => new List<object?[]>
    {
        new object?[] { null },
        new object?[] { new List<UserSettings>() }
    };
}
