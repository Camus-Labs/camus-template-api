using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Configurations;

public class InMemoryModelSettingsTests
{
    // --- Validate ---

    [Fact]
    public void Validate_AllPropertiesValid_DoesNotThrow()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate Roles ---

    [Fact]
    public void Validate_NullRoles_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Roles = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one role*must be defined*");
    }

    [Fact]
    public void Validate_EmptyRoles_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Roles = new List<RoleSettings>();

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
        var settings = CreateValidSettings();
        settings.Roles.Add(new RoleSettings
        {
            Name = "admin",
            Permissions = new List<string> { Permissions.ApiWrite }
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate role name*admin*");
    }

    [Fact]
    public void Validate_InvalidRoleSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
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

    [Fact]
    public void Validate_NullUsers_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Users = null!;

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one user*must be defined*");
    }

    [Fact]
    public void Validate_EmptyUsers_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
        settings.Users = new List<UserSettings>();

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
        var settings = CreateValidSettings();
        settings.Users.Add(new UserSettings
        {
            UsernameSecretName = "admin-username",
            PasswordSecretName = "admin-password-2",
            Roles = new List<string> { "admin" }
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate UsernameSecretName*admin-username*");
    }

    [Fact]
    public void Validate_UserWithInvalidRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
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
        var settings = CreateValidSettings();
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
        var settings = CreateValidSettings();
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
        var settings = CreateValidSettings();
        settings.ApiInfos.Add(new ApiInfoSettings
        {
            Name = "Other API",
            Version = "1.0",
            Status = "Deprecated"
        });

        // Act
        var act = () => settings.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate API version*1.0*");
    }

    [Fact]
    public void Validate_InvalidApiInfoSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings();
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
        var settings = CreateValidSettings();
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

    private static InMemoryModelSettings CreateValidSettings()
    {
        return new InMemoryModelSettings
        {
            Roles = new List<RoleSettings>
            {
                new RoleSettings
                {
                    Name = "admin",
                    Permissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite }
                }
            },
            Users = new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = "admin-username",
                    PasswordSecretName = "admin-password",
                    Roles = new List<string> { "admin" }
                }
            },
            ApiInfos = new List<ApiInfoSettings>
            {
                new ApiInfoSettings
                {
                    Name = "Test API",
                    Version = "1.0",
                    Status = "Available",
                    Features = new List<string> { "feature1" }
                }
            }
        };
    }
}
