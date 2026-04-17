namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Collection definition that shares a single <see cref="ApiTimeoutFactory"/> across all
/// timeout integration test classes. Uses a slow service stub and short timeout policy
/// to test request timeout and client disconnect behavior.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TimeoutTestGroup : ICollectionFixture<ApiTimeoutFactory>
{
    /// <summary>
    /// The collection name shared by all timeout test classes.
    /// </summary>
    public const string Name = "Timeout";
}
