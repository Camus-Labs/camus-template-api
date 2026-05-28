namespace emc.camus.api.Filters;

/// <summary>
/// Specifies which rate limit policy to apply to a controller or action.
/// The policy name must match a policy defined in RateLimitingSettings.Policies configuration.
/// If no attribute is present, the "default" policy is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// The name of the rate limit policy to apply.
    /// Must match a policy defined in configuration.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Creates a new rate limit attribute with the specified policy name.
    /// </summary>
    /// <param name="policyName">The name of the rate limit policy to apply.</param>
    /// <exception cref="ArgumentException">Thrown if policy name is null or whitespace.</exception>
    public RateLimitAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        PolicyName = policyName;
    }
}
