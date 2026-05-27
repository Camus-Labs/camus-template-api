using emc.camus.secrets.dapr.Configurations;

namespace emc.camus.secrets.dapr.test.Helpers;

internal static class DaprSecretProviderSettingsBuilder
{
    private static readonly string[] DefaultSecretNames = ["my-secret"];

    public static DaprSecretProviderSettings CreateValid(
        string baseHost = "localhost",
        string httpPort = "3500",
        string secretStoreName = "my-secret-store",
        int timeoutSeconds = 30,
        List<string>? secretNames = null) => new()
    {
        BaseHost = baseHost,
        HttpPort = httpPort,
        SecretStoreName = secretStoreName,
        TimeoutSeconds = timeoutSeconds,
        SecretNames = secretNames ?? new List<string>(DefaultSecretNames)
    };
}
