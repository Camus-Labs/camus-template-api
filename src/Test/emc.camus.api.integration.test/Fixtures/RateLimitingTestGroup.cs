namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Collection definition that shares a single <see cref="ApiRateLimitingFactory"/> across all
/// rate limiting integration test classes. Prevents parallel <c>WebApplicationFactory&lt;Program&gt;</c>
/// host creation with other factory variants that would cause cross-host interference.
/// </summary>
[CollectionDefinition(Name)]
public sealed class RateLimitingTestGroup : ICollectionFixture<ApiRateLimitingFactory>
{
    /// <summary>
    /// The collection name shared by all rate limiting test classes.
    /// </summary>
    public const string Name = "RateLimiting";
}
