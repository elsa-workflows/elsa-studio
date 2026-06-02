using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Client;
using Elsa.Studio.Dashboard.Menu;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Dashboard.Extensions;

/// <summary>
/// Provides extension methods for configuring dashboard services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the dashboard module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, DashboardMenu>()
            .AddScoped<IDashboardService, DashboardService>()
            .AddDashboardWidget<DashboardWorkflowMetricsWidget>("dashboard.workflow.metrics", DashboardWidgetZones.Metrics, 100, "Workflow metrics", payloadKind: "WorkflowInstances")
            .AddDashboardWidget<DashboardNeedsAttentionWidget>("dashboard.needs-attention", DashboardWidgetZones.Findings, 100, "Needs attention")
            .AddDashboardWidget<DashboardTrendWidget>("dashboard.workflow.trend", DashboardWidgetZones.PrimaryPanels, 100, "Workflow trends", payloadKind: "WorkflowTrends")
            .AddDashboardWidget<DashboardRecentActivityWidget>("dashboard.workflow.recent-activity", DashboardWidgetZones.PrimaryPanels, 200, "Recent activity", payloadKind: "RecentActivity")
            .AddDashboardWidget<DashboardWorkflowHotspotsWidget>("dashboard.workflow.hotspots", DashboardWidgetZones.SecondaryPanels, 100, "Workflow hotspots", payloadKind: "WorkflowHotspots");
    }

    /// <summary>
    /// Adds the dashboard module services with remote backend dashboard API support.
    /// </summary>
    public static IServiceCollection AddDashboardModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddDashboardModule()
            .AddRemoteApi<IDashboardApi>(backendApiConfig);
    }
}
