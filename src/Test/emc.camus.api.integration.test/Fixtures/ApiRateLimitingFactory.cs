using System.Globalization;
using Microsoft.AspNetCore.Hosting;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant for integration testing rate limiting behavior.
/// Configures distinct permit limits per policy to verify IP-based partitioning,
/// request throttling, and policy-specific enforcement through the full HTTP pipeline.
/// </summary>
public class ApiRateLimitingFactory : ApiFactoryBase
{
    /// <summary>
    /// The permit limit configured for the strict policy (e.g., auth endpoints).
    /// </summary>
    public const int StrictPolicyPermitLimit = 2;

    /// <summary>
    /// The permit limit configured for the default policy (e.g., authenticated info endpoints).
    /// </summary>
    public const int DefaultPolicyPermitLimit = 3;

    /// <summary>
    /// The permit limit configured for the relaxed policy (e.g., public info endpoints).
    /// </summary>
    public const int RelaxedPolicyPermitLimit = 5;

    /// <summary>
    /// The window duration in seconds configured for all policies in this factory.
    /// Kept short (2 seconds) to enable testing window reset behavior without slow tests.
    /// </summary>
    public const int PolicyWindowSeconds = 2;

    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("DataPersistenceSettings:Provider", "InMemory");
        builder.UseSetting("DBUpSettings:Enabled", "false");

        // Override rate limits with distinct values per policy to verify policy-specific enforcement
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:default:PermitLimit", DefaultPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:default:WindowSeconds", PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:strict:PermitLimit", StrictPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:strict:WindowSeconds", PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:relaxed:PermitLimit", RelaxedPolicyPermitLimit.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("InMemoryRateLimitingSettings:Policies:relaxed:WindowSeconds", PolicyWindowSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
