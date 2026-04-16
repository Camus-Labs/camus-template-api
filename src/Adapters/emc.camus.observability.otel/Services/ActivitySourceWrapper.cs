using System;
using System.Diagnostics;
using System.Threading.Tasks;
using emc.camus.application.Observability;

namespace emc.camus.observability.otel.Services
{
    /// <summary>
    /// OpenTelemetry-based implementation of activity source wrapper for distributed tracing.
    /// </summary>
    internal sealed class ActivitySourceWrapper : IActivitySourceWrapper
    {
        private readonly ActivitySource _activitySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivitySourceWrapper"/> class.
        /// </summary>
        /// <param name="activitySource">The underlying ActivitySource for creating activities.</param>
        /// <exception cref="ArgumentNullException">Thrown when activitySource is null.</exception>
        public ActivitySourceWrapper(ActivitySource activitySource)
        {
            ArgumentNullException.ThrowIfNull(activitySource);

            _activitySource = activitySource;
        }

        /// <summary>
        /// Starts an activity with required standard tags: operation.type and operation.success=false.
        /// </summary>
        /// <param name="name">The activity name.</param>
        /// <param name="operationType">The operation type (required).</param>
        /// <returns>The started activity with standard tags set.</returns>
        public Activity? StartActivity(string name, OperationType operationType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!Enum.IsDefined(operationType))
                throw new ArgumentOutOfRangeException(nameof(operationType), operationType, "Invalid operation type.");

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
        /// Sets tags on the activity, prefixing each key with 'request.'
        /// </summary>
        /// <param name="activity">The activity to add request tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'request.' prefix.</param>
        public void SetRequestTags(Activity? activity, IDictionary<string, object?> tags)
        {
            ArgumentNullException.ThrowIfNull(tags);
            SetTagsWithPrefix(activity, tags, "request");
        }

        /// <summary>
        /// Sets tags on the activity, prefixing each key with 'execution.'
        /// </summary>
        /// <param name="activity">The activity to add execution tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'execution.' prefix.</param>
        public void SetExecutionTags(Activity? activity, IDictionary<string, object?> tags)
        {
            ArgumentNullException.ThrowIfNull(tags);
            SetTagsWithPrefix(activity, tags, "execution");
        }

        /// <summary>
        /// Sets tags on the activity, prefixing each key with 'response.'
        /// </summary>
        /// <param name="activity">The activity to add response tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add with 'response.' prefix.</param>
        public void SetResponseTags(Activity? activity, IDictionary<string, object?> tags)
        {
            ArgumentNullException.ThrowIfNull(tags);
            SetTagsWithPrefix(activity, tags, "response");
        }

        /// <summary>
        /// Helper method to set tags with a required prefix.
        /// </summary>
        /// <param name="activity">The activity to add tags to.</param>
        /// <param name="tags">Dictionary of tag key-value pairs to add.</param>
        /// <param name="prefix">Required prefix to prepend to each tag key.</param>
        private static void SetTagsWithPrefix(Activity? activity, IDictionary<string, object?> tags, string prefix)
        {
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    var key = $"{prefix}.{tag.Key}";
                    activity.SetTag(key, tag.Value);
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
            ArgumentNullException.ThrowIfNull(ex);
            if (activity == null) return;
            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", ex.Message);
            AddEvent(activity, "exception", new Dictionary<string, object?>
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty }
            });
        }

        /// <summary>
        /// Adds a timestamped event with optional tags to the activity.
        /// </summary>
        /// <param name="activity">The activity to add the event to.</param>
        /// <param name="name">The event name.</param>
        /// <param name="tags">Optional tags to attach to the event.</param>
        public void AddEvent(Activity? activity, string name, IDictionary<string, object?>? tags = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (activity == null) return;

            var eventTags = new ActivityTagsCollection();
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    eventTags.Add(tag.Key, tag.Value);
                }
            }
            activity.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, eventTags));
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
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!Enum.IsDefined(operationType))
                throw new ArgumentOutOfRangeException(nameof(operationType), operationType, "Invalid operation type.");
            ArgumentNullException.ThrowIfNull(func);

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
