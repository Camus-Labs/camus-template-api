using System;
using System.Diagnostics;
using System.Threading.Tasks;
using emc.camus.application.Observability;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// OpenTelemetry-based implementation of activity source wrapper for distributed tracing.
    /// </summary>
    public class ActivitySourceWrapper : IActivitySourceWrapper
    {
        private readonly ActivitySource _activitySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivitySourceWrapper"/> class.
        /// </summary>
        /// <param name="activitySource">The underlying ActivitySource for creating activities.</param>
        /// <exception cref="ArgumentNullException">Thrown when activitySource is null.</exception>
        public ActivitySourceWrapper(ActivitySource activitySource)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        /// <summary>
        /// Starts an activity with required standard tags: operation.type and operation.success=false.
        /// </summary>
        /// <param name="name">The activity name.</param>
        /// <param name="operationType">The operation type (required).</param>
        /// <returns>The started activity with standard tags set.</returns>
        public Activity? StartActivity(string name, OperationType operationType)
        {
            var activity = _activitySource.StartActivity(name);
            if (activity != null)
            {
                activity.SetTag("operation.type", operationType.ToString().ToLowerInvariant());
                // Use standard OpenTelemetry status codes: start as UNSET
                activity.SetTag("otel.status_code", "UNSET");
            }
            return activity;
        }

        /// <summary>
        /// Sets a tag on the given activity if not null.
        /// </summary>
        /// <param name="activity">The activity to add the tag to.</param>
        /// <param name="key">The tag key.</param>
        /// <param name="value">The tag value.</param>
        public void SetTag(Activity? activity, string key, object? value)
        {
            activity?.SetTag(key, value);
        }

        /// <summary>
        /// Sets multiple tags on the given activity from a dictionary.
        /// </summary>
        /// <param name="activity">The activity to add tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add.</param>
        public void SetTags(Activity? activity, IDictionary<string, object?> tags)
        {
            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Sets tags on the activity, prefixing each key with 'request.'
        /// </summary>
        /// <param name="activity">The activity to add request tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'request.' prefix.</param>
        public void SetRequestTags(Activity? activity, IDictionary<string, object?> tags)
        {
            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag($"request.{tag.Key}", tag.Value);
                }
            }
        }

        /// <summary>
        /// Sets tags on the activity, prefixing each key with 'response.'
        /// </summary>
        /// <param name="activity">The activity to add response tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'response.' prefix.</param>
        public void SetResponseTags(Activity? activity, IDictionary<string, object?> tags)
        {
            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag($"response.{tag.Key}", tag.Value);
                }
            }
        }
        /// <summary>
        /// Marks the activity as succeeded, sets operation.success = true, and adds a Succeeded event.
        /// </summary>
        /// <param name="activity">The activity to mark as succeeded.</param>
        public void ActivitySucceeded(Activity? activity)
        {
            if (activity == null) return;
            // Set OpenTelemetry span status via standard tag
            activity.SetTag("otel.status_code", "OK");
        }

        /// <summary>
        /// Marks the activity as failed, sets status to ERROR with description, and adds a Failed event.
        /// </summary>
        /// <param name="activity">The activity to mark as failed.</param>
        /// <param name="ex">The exception that caused the failure.</param>
        public void ActivityFailed(Activity? activity, Exception ex)
        {
            if (activity == null) return;
            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", ex?.Message);
            // Add an 'exception' event with attributes for better trace correlation
            var exceptionTags = new ActivityTagsCollection
            {
                { "exception.type", ex?.GetType().FullName },
                { "exception.message", ex?.Message },
                { "exception.stacktrace", ex?.StackTrace ?? string.Empty }
            };
            activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, exceptionTags));
        }


        /// <summary>
        /// Starts an activity and executes the provided async function returning a value, marking success or failure automatically.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The activity name.</param>
        /// <param name="operationType">The operation type.</param>
        /// <param name="func">The async function to execute within the activity context.</param>
        /// <returns>The result of the executed function.</returns>
        public async Task<T> StartActivityAndRunAsync<T>(string name, OperationType operationType, Func<Activity?, Task<T>> func)
        {
            using var activity = StartActivity(name, operationType);
            try
            {
                var result = await func(activity).ConfigureAwait(false);
                ActivitySucceeded(activity);
                return result;
            }
            catch (Exception ex)
            {
                ActivityFailed(activity, ex);
                throw;
            }
        }
    }
}
