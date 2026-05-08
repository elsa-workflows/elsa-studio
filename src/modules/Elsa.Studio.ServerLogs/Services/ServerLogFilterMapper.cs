using Elsa.Studio.ServerLogs.Models;

namespace Elsa.Studio.ServerLogs.Services;

/// <summary>
/// Maps UI filter state to backend request filters.
/// </summary>
public static class ServerLogFilterMapper
{
    /// <summary>
    /// Creates a filter for recent-log requests and clamps the requested row count to the UI cap.
    /// </summary>
    public static ServerLogFilter ToRecentRequest(ServerLogFilter filter, int rowCap)
    {
        var request = Copy(filter);
        request.Take = Math.Clamp(request.Take ?? rowCap, 1, rowCap);
        return request;
    }

    /// <summary>
    /// Creates a filter for live SignalR subscriptions.
    /// </summary>
    public static ServerLogFilter ToLiveSubscription(ServerLogFilter filter) => Copy(filter);

    private static ServerLogFilter Copy(ServerLogFilter filter) =>
        new()
        {
            MinimumLevel = filter.MinimumLevel,
            Levels = filter.Levels?.ToList(),
            CategoryPrefix = filter.CategoryPrefix,
            Text = filter.Text,
            TenantId = filter.TenantId,
            WorkflowDefinitionId = filter.WorkflowDefinitionId,
            WorkflowInstanceId = filter.WorkflowInstanceId,
            TraceId = filter.TraceId,
            CorrelationId = filter.CorrelationId,
            SourceId = filter.SourceId,
            From = filter.From,
            To = filter.To,
            Take = filter.Take
        };
}
