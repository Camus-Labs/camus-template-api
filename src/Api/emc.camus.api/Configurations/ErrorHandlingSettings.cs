namespace emc.camus.api.Configurations;

/// <summary>
/// Configuration settings for error handling and error code resolution.
/// Defines additional mappings from exceptions to machine-readable error codes beyond the platform defaults.
/// </summary>
public class ErrorHandlingSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string ConfigurationSectionName = "ErrorHandlingSettings";

    /// <summary>
    /// Optional additional error mapping rules that supplement the platform-defined rules.
    /// These rules are evaluated before platform rules, allowing configuration overrides.
    /// Rules can match on exception type, message pattern, or both.
    /// </summary>
    public List<ErrorCodeMappingRule> AdditionalRules { get; set; } = new();

    /// <summary>
    /// Validates the error handling settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateAdditionalRules();
    }

    private void ValidateAdditionalRules()
    {
        if (AdditionalRules == null)
        {
            throw new InvalidOperationException($"AdditionalRules cannot be null.");
        }

        for (int i = 0; i < AdditionalRules.Count; i++)
        {
            AdditionalRules[i].Validate(i);
        }
    }
}
