using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace emc.camus.domain.Logging
{
    /// <summary>
    /// IActivitySourceWrapper
    /// </summary>
    public interface IActivitySourceWrapper
    {
    /// <summary>
    /// StartActivity with required operationType.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="operationType">The operation type (required).</param>
    /// <returns>The started activity with standard tags set.</returns>
    Activity? StartActivity(string name, OperationType operationType);

    /// <summary>
    /// Sets tags on the activity, prefixing each key with 'request.'
    /// </summary>
    void SetRequestTags(Activity? activity, IDictionary<string, object?> tags);

    /// <summary>
    /// Sets tags on the activity, prefixing each key with 'response.'
    /// </summary>
    void SetResponseTags(Activity? activity, IDictionary<string, object?> tags);

    /// <summary>
    /// Marks the activity as succeeded and adds a standardized event.
    /// </summary>
    void ActivitySucceeded(Activity? activity);

    /// <summary>
    /// Marks the activity as failed with an exception and adds a standardized event.
    /// Also sets the OpenTelemetry status description.
    /// </summary>
    void ActivityFailed(Activity? activity, Exception ex);

    /// <summary>
    /// Starts an activity and executes the provided async function returning a value, marking success or failure automatically.
    /// </summary>
    Task<T> StartActivityAndRunAsync<T>(string name, OperationType operationType, Func<Activity?, Task<T>> func);
    }
}