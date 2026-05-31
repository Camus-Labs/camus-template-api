using emc.camus.api.Configurations;

namespace emc.camus.api.Filters;

/// <summary>
/// Specifies which rate limit policy to apply to a controller or action.
/// The policy name must be one of the closed set defined in <see cref="RateLimitPolicies"/>.
/// If no attribute is present, the "default" policy is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// The name of the rate limit policy to apply.
    /// Must be a value from <see cref="RateLimitPolicies"/>.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Creates a new rate limit attribute with the specified policy name.
    /// </summary>
    /// <param name="policyName">The name of the rate limit policy to apply.</param>
    /// <exception cref="ArgumentException">Thrown if policy name is null, whitespace, or not a recognized policy.</exception>
    public RateLimitAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ValidatePolicyName(policyName);

        PolicyName = policyName;
    }

    private static void ValidatePolicyName(string policyName)
    {
        if (policyName != RateLimitPolicies.Default &&
            policyName != RateLimitPolicies.Strict &&
            policyName != RateLimitPolicies.Relaxed)
        {
            throw new ArgumentException(
                $"PolicyName '{policyName}' is not a recognized rate limit policy. " +
                $"Use {nameof(RateLimitPolicies)}.{nameof(RateLimitPolicies.Default)}, " +
                $"{nameof(RateLimitPolicies)}.{nameof(RateLimitPolicies.Strict)}, or " +
                $"{nameof(RateLimitPolicies)}.{nameof(RateLimitPolicies.Relaxed)}.",
                nameof(policyName));
        }
    }
}
