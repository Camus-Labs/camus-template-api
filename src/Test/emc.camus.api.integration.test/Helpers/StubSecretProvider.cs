using emc.camus.application.Secrets;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Stub <see cref="ISecretProvider"/> that returns deterministic test values.
/// Replaces Dapr secret provider in integration tests to avoid requiring a Dapr sidecar.
/// </summary>
public sealed class StubSecretProvider : ISecretProvider
{
    private static readonly Dictionary<string, string> Secrets = new()
    {
        ["AdminUser"] = "admin",
        ["AdminSecret"] = "admin-password",
        ["ClientAppUser"] = "client",
        ["ClientAppSecret"] = "client-password",
        ["XApiKey"] = "test-api-key-value",
        ["RsaPrivateKeyPem"] = GenerateTestRsaKey(),
        ["DBUser"] = "postgres",
        ["DBSecret"] = "postgres",
        ["DBMigrationsUser"] = "postgres",
        ["DBMigrationsSecret"] = "postgres",
    };

    public Task LoadSecretsAsync(IEnumerable<string> secretNames, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public string GetSecret(string name)
    {
        return Secrets.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Test secret '{name}' not configured.");
    }

    private static string GenerateTestRsaKey()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();
        return pem;
    }
}
