using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Workflows.Dashboard.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class WorkflowDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        var registry = serviceProvider.GetService<IDashboardWidgetRegistry>();
        if (registry == null)
            return ValueTask.CompletedTask;

        foreach (var descriptor in Descriptors)
            registry.Add(descriptor);

        return ValueTask.CompletedTask;
    }

    private static IReadOnlyCollection<DashboardWidgetDescriptor> Descriptors =>
    [
        new()
        {
            Id = "workflows.metrics",
            Title = "Workflow metrics",
            ComponentType = typeof(WorkflowMetricsDashboardWidget),
            Zone = DashboardWidgetZone.Metrics,
            Order = 0,
            Span = DashboardWidgetSpan.Full,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        },
        new()
        {
            Id = "workflows.needs-attention",
            Title = "Needs attention",
            ComponentType = typeof(WorkflowNeedsAttentionDashboardWidget),
            Zone = DashboardWidgetZone.Primary,
            Order = 0,
            Span = DashboardWidgetSpan.Compact,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        },
        new()
        {
            Id = "workflows.execution-trend",
            Title = "Execution trend",
            ComponentType = typeof(WorkflowTrendDashboardWidget),
            Zone = DashboardWidgetZone.Primary,
            Order = 10,
            Span = DashboardWidgetSpan.Compact,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        },
        new()
        {
            Id = "workflows.hotspots",
            Title = "Workflow hotspots",
            ComponentType = typeof(WorkflowHotspotsDashboardWidget),
            Zone = DashboardWidgetZone.Primary,
            Order = 20,
            Span = DashboardWidgetSpan.Compact,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        },
        new()
        {
            Id = "workflows.recent-activity",
            Title = "Recent activity",
            ComponentType = typeof(WorkflowRecentActivityDashboardWidget),
            Zone = DashboardWidgetZone.Secondary,
            Order = 0,
            Span = DashboardWidgetSpan.Full,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        }
    ];
}
