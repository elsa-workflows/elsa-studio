using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Maps UI filter state to backend request filters.
/// </summary>
public static class ConsoleLogFilterMapper
{
    /// <summary>
    /// Creates a filter for recent-line requests and clamps the requested row count to the UI cap.
    /// </summary>
    public static ConsoleLogFilter ToRecentRequest(ConsoleLogFilter filter, int rowCap)
    {
        var request = Copy(filter);
        request.Take = Math.Clamp(request.Take ?? rowCap, 1, rowCap);
        return request;
    }

    /// <summary>
    /// Creates a filter for live SignalR subscriptions.
    /// </summary>
    public static ConsoleLogFilter ToLiveSubscription(ConsoleLogFilter filter) => Copy(filter);

    private static ConsoleLogFilter Copy(ConsoleLogFilter filter) =>
        new()
        {
            SourceId = filter.SourceId,
            Streams = filter.Streams?.ToList(),
            Text = filter.Text,
            From = filter.From,
            To = filter.To,
            Take = filter.Take
        };
}
