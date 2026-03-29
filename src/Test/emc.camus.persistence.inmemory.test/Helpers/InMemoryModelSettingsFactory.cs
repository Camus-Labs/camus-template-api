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

    internal static InMemoryModelSettings Create(
        List<RoleSettings>? roles = null,
        List<UserSettings>? users = null,
        List<ApiInfoSettings>? apiInfos = null)
    {
        return new InMemoryModelSettings
        {
            Roles = roles ?? new List<RoleSettings>
            {
                new RoleSettings
                {
                    Name = DefaultRoleName,
                    Permissions = new List<string> { Permissions.ApiRead, Permissions.ApiWrite }
                }
            },
            Users = users ?? new List<UserSettings>
            {
                new UserSettings
                {
                    UsernameSecretName = DefaultUsernameSecret,
                    PasswordSecretName = DefaultPasswordSecret,
                    Roles = new List<string> { DefaultRoleName }
                }
            },
            ApiInfos = apiInfos ?? new List<ApiInfoSettings>
            {
                new ApiInfoSettings
                {
                    Name = DefaultApiName,
                    Version = DefaultApiVersion,
                    Status = DefaultApiStatus,
                    Features = new List<string> { DefaultApiFeature }
                }
            }
        };
    }
}
