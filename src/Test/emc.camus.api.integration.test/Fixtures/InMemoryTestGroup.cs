namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Collection definition that shares a single <see cref="CamusApiIMFactory"/> across all
/// in-memory integration test classes. Prevents parallel <c>WebApplicationFactory&lt;Program&gt;</c>
/// host creation with other factory variants (e.g., PostgreSQL) that would cause cross-host interference.
/// </summary>
[CollectionDefinition(Name)]
public sealed class InMemoryTestGroup : ICollectionFixture<CamusApiIMFactory>
{
    /// <summary>
    /// The collection name shared by all in-memory test classes.
    /// </summary>
    public const string Name = "InMemory";
}
