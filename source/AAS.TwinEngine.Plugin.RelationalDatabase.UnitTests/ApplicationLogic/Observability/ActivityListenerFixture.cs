using System.Diagnostics;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Observability;

/// <summary>
/// Provides centralized ActivityListener lifecycle management for unit tests.
/// Ensures listeners are properly disposed and activities are cleared between tests,
/// preventing state pollution and test isolation issues.
/// </summary>
public sealed class ActivityListenerFixture : IDisposable
{
    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public ActivityListenerFixture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == PluginTracing.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _activities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Gets the list of captured activities for the current test.
    /// </summary>
    public List<Activity> Activities => _activities;

    public void Dispose()
    {
        _activities.Clear();
        _listener?.Dispose();
    }
}
