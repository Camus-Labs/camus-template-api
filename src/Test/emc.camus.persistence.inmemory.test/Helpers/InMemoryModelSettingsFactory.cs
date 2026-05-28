using emc.camus.application.Auth;
using emc.camus.persistence.inmemory.Configurations;

namespace emc.camus.persistence.inmemory.test.Helpers;

internal static class InMemoryModelSettingsFactory
{
    internal const string DefaultRoleName = "admin";
    internal const string DefaultUsernameSecret = "admin-username";
    internal const string DefaultPasswordSecret = "admin-password";
    internal const string DefaultApiName = "Test API";
    internal const string DefaultApiVersion = "1.0";
    internal const string DefaultApiStatus = "Available";
    internal const string DefaultApiFeature = "feature1";

    private static readonly List<string> DefaultPermissions = new() { Permissions.ApiRead, Permissions.ApiWrite };
    private static readonly List<string> DefaultRoleNameList = new() { DefaultRoleName };
    private static readonly List<string> DefaultFeatureList = new() { DefaultApiFeature };

    private static readonly List<RoleSettings> DefaultRoles = new()
    {
        new RoleSettings
        {
            Name = DefaultRoleName,
            Permissions = DefaultPermissions
        }
    };

    private static readonly List<UserSettings> DefaultUsers = new()
    {
        new UserSettings
        {
            UsernameSecretName = DefaultUsernameSecret,
            PasswordSecretName = DefaultPasswordSecret,
            Roles = DefaultRoleNameList
        }
    };

    private static readonly List<ApiInfoSettings> DefaultApiInfos = new()
    {
        new ApiInfoSettings
        {
            Name = DefaultApiName,
            Version = DefaultApiVersion,
            Status = DefaultApiStatus,
            Features = DefaultFeatureList
        }
    };

    internal static InMemoryModelSettings Create(
        List<RoleSettings>? roles = null,
        List<UserSettings>? users = null,
        List<ApiInfoSettings>? apiInfos = null)
    {
        return new InMemoryModelSettings
        {
            Roles = roles ?? new List<RoleSettings>(DefaultRoles),
            Users = users ?? new List<UserSettings>(DefaultUsers),
            ApiInfos = apiInfos ?? new List<ApiInfoSettings>(DefaultApiInfos)
        };
    }
}
