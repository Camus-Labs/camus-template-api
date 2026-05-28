namespace emc.camus.api.Configurations;

/// <summary>
/// Represents a single error code mapping rule for exception-to-error-code resolution.
/// Rules are evaluated in order until a match is found.
/// </summary>
public class ErrorCodeMappingRuleSettings
{
    private const int MaxErrorCodeLength = 50;
    private const int MaxTypeLength = 100;
    private const int MaxPatternLength = 500;

    /// <summary>
    /// Optional exception type name to match (e.g., "ArgumentException", "UnauthorizedAccessException").
    /// If null, pattern will be checked against all exception types.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Optional regex pattern to match against exception message.
    /// If null, only exception type will be checked.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Error code to return when this rule matches (e.g., "invalid_credentials", "bad_request").
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// The positional index of this rule within its parent collection, used for error messages.
    /// Set by the parent <see cref="ErrorHandlingSettings"/> before calling <see cref="Validate"/>.
    /// </summary>
    internal int RuleIndex { get; set; }

    /// <summary>
    /// Validates the error code mapping rule.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any property is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateErrorCode();
        ValidateTypeOrPattern();
        ValidateType();
        ValidatePattern();
    }

    private void ValidateErrorCode()
    {
        if (string.IsNullOrWhiteSpace(ErrorCode))
        {
            throw new InvalidOperationException(
                $"Rules[{RuleIndex}].ErrorCode cannot be null or empty.");
        }

        if (ErrorCode.Length > MaxErrorCodeLength)
        {
            throw new InvalidOperationException(
                $"Rules[{RuleIndex}].ErrorCode must not exceed {MaxErrorCodeLength} characters. Current length: {ErrorCode.Length}");
        }
    }

    private void ValidateTypeOrPattern()
    {
        if (string.IsNullOrWhiteSpace(Type) && string.IsNullOrWhiteSpace(Pattern))
        {
            throw new InvalidOperationException(
                $"Rules[{RuleIndex}] must have either Type or Pattern specified. Got Type: '{Type}', Pattern: '{Pattern}'.");
        }
    }

    private void ValidateType()
    {
        if (!string.IsNullOrWhiteSpace(Type) && Type.Length > MaxTypeLength)
        {
            throw new InvalidOperationException(
                $"Rules[{RuleIndex}].Type must not exceed {MaxTypeLength} characters. Current length: {Type.Length}");
        }
    }

    private void ValidatePattern()
    {
        if (!string.IsNullOrWhiteSpace(Pattern) && Pattern.Length > MaxPatternLength)
        {
            throw new InvalidOperationException(
                $"Rules[{RuleIndex}].Pattern must not exceed {MaxPatternLength} characters. Current length: {Pattern.Length}");
        }
    }
}
