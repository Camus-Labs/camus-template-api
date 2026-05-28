using System.Diagnostics;
using emc.camus.application.Observability;

namespace emc.camus.api.test.Helpers;

internal sealed class FakeActivitySourceWrapper : IActivitySourceWrapper
{
    public List<IDictionary<string, object?>> RequestTagsCalls { get; }
    public List<IDictionary<string, object?>> ResponseTagsCalls { get; }

    public FakeActivitySourceWrapper()
    {
        RequestTagsCalls = new List<IDictionary<string, object?>>();
        ResponseTagsCalls = new List<IDictionary<string, object?>>();
    }

    public Activity? StartActivity(string name, OperationType operationType) => null;

    public void SetRequestTags(Activity? activity, IDictionary<string, object?> tags)
        => RequestTagsCalls.Add(tags);

    public void SetExecutionTags(Activity? activity, IDictionary<string, object?> tags) { }

    public void SetResponseTags(Activity? activity, IDictionary<string, object?> tags)
        => ResponseTagsCalls.Add(tags);

    public void ActivitySucceeded(Activity? activity) { }

    public void ActivityFailed(Activity? activity, Exception ex) { }

    public void ActivityCancelled(Activity? activity) { }

    public void AddEvent(Activity? activity, string name, IDictionary<string, object?>? tags = null) { }

    public async Task<T> StartActivityAndRunAsync<T>(string name, OperationType operationType, Func<Activity?, Task<T>> func)
        => await func(null);
}
