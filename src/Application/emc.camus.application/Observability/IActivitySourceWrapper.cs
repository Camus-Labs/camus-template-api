using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace emc.camus.application.Observability
{
    /// <summary>
    /// Provides abstraction for creating and managing distributed tracing activities.
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
        /// <param name="activity">The activity to add request tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'request.' prefix.</param>
        void SetRequestTags(Activity? activity, IDictionary<string, object?> tags);

        /// <summary>
        /// Sets tags on the activity, prefixing each key with 'response.'
        /// </summary>
        /// <param name="activity">The activity to add response tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'response.' prefix.</param>
        void SetResponseTags(Activity? activity, IDictionary<string, object?> tags);

        /// <summary>
        /// Marks the activity as succeeded and adds a standardized event.
        /// </summary>
        /// <param name="activity">The activity to mark as succeeded.</param>
        void ActivitySucceeded(Activity? activity);

        /// <summary>
        /// Marks the activity as failed with an exception and adds a standardized event.
        /// Also sets the OpenTelemetry status description.
        /// </summary>
        /// <param name="activity">The activity to mark as failed.</param>
        /// <param name="ex">The exception that caused the failure.</param>
        void ActivityFailed(Activity? activity, Exception ex);

        /// <summary>
        /// Starts an activity and executes the provided async function returning a value, marking success or failure automatically.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The activity name.</param>
        /// <param name="operationType">The operation type.</param>
        /// <param name="func">The async function to execute within the activity context.</param>
        /// <returns>The result of the executed function.</returns>
        Task<T> StartActivityAndRunAsync<T>(string name, OperationType operationType, Func<Activity?, Task<T>> func);
    }
}
