namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Collection definition that shares a single <see cref="ApiSwaggerFactory"/> across all
/// Swagger integration test classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SwaggerTestGroup : ICollectionFixture<ApiSwaggerFactory>
{
    /// <summary>
    /// The collection name shared by all Swagger test classes.
    /// </summary>
    public const string Name = "Swagger";
}
