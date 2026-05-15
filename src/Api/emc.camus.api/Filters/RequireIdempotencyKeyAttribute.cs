namespace emc.camus.api.Filters;

/// <summary>
/// Marks a controller action as requiring an Idempotency-Key header.
/// When applied, the idempotency validation filter will enforce the presence
/// and format of the header before the action executes.
/// </summary>
/// <example>
/// [RequireIdempotencyKey(IdempotencyPolicies.Default)] - Apply "default" policy (5 min TTL)
/// [RequireIdempotencyKey(IdempotencyPolicies.LongTerm)] - Apply "long-term" policy (24 hour TTL)
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireIdempotencyKeyAttribute : Attribute
{
    /// <summary>
    /// The name of the idempotency TTL policy to apply.
    /// Must match a policy defined in configuration.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Creates a new idempotency key requirement attribute with the specified policy name.
    /// </summary>
    /// <param name="policyName">The name of the idempotency TTL policy to apply.</param>
    /// <exception cref="ArgumentException">Thrown if policy name is null or whitespace.</exception>
    public RequireIdempotencyKeyAttribute(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        PolicyName = policyName;
    }
}
