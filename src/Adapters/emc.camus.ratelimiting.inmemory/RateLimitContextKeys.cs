using System.Diagnostics.CodeAnalysis;

namespace emc.camus.ratelimiting.inmemory
{
    [ExcludeFromCodeCoverage]
    internal static class RateLimitContextKeys
    {
        internal const string Policy = "RateLimit:Policy";
        internal const string Limit = "RateLimit:Limit";
        internal const string Window = "RateLimit:Window";
    }
}
