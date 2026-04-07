namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Collection definition that shares a single <see cref="CamusApiPSFactory"/> (and its
/// Testcontainer) across all PostgreSQL integration test classes. Without this, each class
/// using <c>IClassFixture&lt;CamusApiPSFactory&gt;</c> would start its own container and host,
/// causing parallel <c>WebApplicationFactory&lt;Program&gt;</c> interference.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestGroup : ICollectionFixture<CamusApiPSFactory>
{
    /// <summary>
    /// The collection name shared by all PostgreSQL test classes.
    /// </summary>
    public const string Name = "PostgreSQL";
}
