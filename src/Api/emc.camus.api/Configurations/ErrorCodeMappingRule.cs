namespace emc.camus.api.Configurations;

/// <summary>
/// Represents a single error code mapping rule for exception-to-error-code resolution.
/// Rules are evaluated in order until a match is found.
/// </summary>
public class ErrorCodeMappingRule
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
    /// Validates the error code mapping rule.
    /// </summary>
    /// <param name="index">The index of this rule in the Rules collection, used for error messages.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any property is invalid.
    /// </exception>
    public void Validate(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        ValidateErrorCode(index);
        ValidateTypeOrPattern(index);
        ValidateType(index);
        ValidatePattern(index);
    }

    private void ValidateErrorCode(int index)
    {
        if (string.IsNullOrWhiteSpace(ErrorCode))
        {
            throw new InvalidOperationException(
                $"Rules[{index}].ErrorCode cannot be null or empty.");
        }

        if (ErrorCode.Length > MaxErrorCodeLength)
        {
            throw new InvalidOperationException(
                $"Rules[{index}].ErrorCode must not exceed {MaxErrorCodeLength} characters. Current length: {ErrorCode.Length}");
        }
    }

    private void ValidateTypeOrPattern(int index)
    {
        if (string.IsNullOrWhiteSpace(Type) && string.IsNullOrWhiteSpace(Pattern))
        {
            throw new InvalidOperationException(
                $"Rules[{index}] must have either Type or Pattern specified. Got Type: '{Type}', Pattern: '{Pattern}'.");
        }
    }

    private void ValidateType(int index)
    {
        if (!string.IsNullOrWhiteSpace(Type) && Type.Length > MaxTypeLength)
        {
            throw new InvalidOperationException(
                $"Rules[{index}].Type must not exceed {MaxTypeLength} characters. Current length: {Type.Length}");
        }
    }

    private void ValidatePattern(int index)
    {
        if (!string.IsNullOrWhiteSpace(Pattern) && Pattern.Length > MaxPatternLength)
        {
            throw new InvalidOperationException(
                $"Rules[{index}].Pattern must not exceed {MaxPatternLength} characters. Current length: {Pattern.Length}");
        }
    }
}
