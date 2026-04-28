using System.Collections.Concurrent;
using System.Diagnostics;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Captures <see cref="Activity"/> instances from the ASP.NET Core instrumentation source
/// during integration tests. Subscribes to all activity sources and stores stopped activities
/// that originate from the <c>Microsoft.AspNetCore</c> source so tests can inspect enrichment tags.
/// </summary>
public sealed class ActivityCapture : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _activities = new();

    public ActivityCapture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity),
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Returns all captured activities from the <c>Microsoft.AspNetCore</c> source.
    /// </summary>
    public IReadOnlyList<Activity> GetActivities() => _activities.ToArray();

    /// <summary>
    /// Returns the single captured activity whose <c>http.route</c> tag contains the specified path segment.
    /// Throws if zero or more than one activity matches.
    /// </summary>
    public Activity GetSingleByRoute(string routeFragment)
    {
        var matching = _activities
            .Where(a =>
            {
                var route = a.GetTagItem("http.route")?.ToString();
                return route != null && route.Contains(routeFragment, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (matching.Count == 0)
        {
            var allRoutes = string.Join("; ", _activities.Select(a =>
                $"Source={a.Source.Name}, DisplayName={a.DisplayName}, http.route={a.GetTagItem("http.route") ?? "(null)"}"));

            throw new InvalidOperationException(
                $"No activity found with http.route containing '{routeFragment}'. " +
                $"Captured {_activities.Count} activities: [{allRoutes}]");
        }

        if (matching.Count > 1)
        {
            throw new InvalidOperationException(
                $"Expected 1 activity with http.route containing '{routeFragment}' but found {matching.Count}.");
        }

        return matching[0];
    }

    /// <summary>
    /// Returns the single captured activity whose <see cref="Activity.DisplayName"/> matches the specified name.
    /// Throws if zero or more than one activity matches.
    /// </summary>
    public Activity GetSingleByDisplayName(string displayName)
    {
        var matching = _activities
            .Where(a => a.DisplayName == displayName)
            .ToList();

        if (matching.Count == 0)
        {
            var all = string.Join("; ", _activities.Select(a =>
                $"Source={a.Source.Name}, DisplayName={a.DisplayName}"));

            throw new InvalidOperationException(
                $"No activity found with DisplayName '{displayName}'. " +
                $"Captured {_activities.Count} activities: [{all}]");
        }

        if (matching.Count > 1)
        {
            throw new InvalidOperationException(
                $"Expected 1 activity with DisplayName '{displayName}' but found {matching.Count}.");
        }

        return matching[0];
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
