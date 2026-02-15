using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Common;

/// <summary>
/// Defines custom media type (Content-Type) values for the application.
/// For standard media types, use System.Net.Mime.MediaTypeNames from the framework.
/// </summary>
[ExcludeFromCodeCoverage]
public static class MediaTypes
{
    /// <summary>
    /// Media type for RFC 7807 Problem Details responses.
    /// </summary>
    public const string ProblemJson = "application/problem+json";
}
