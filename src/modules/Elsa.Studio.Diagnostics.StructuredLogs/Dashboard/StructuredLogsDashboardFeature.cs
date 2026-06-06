using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class StructuredLogsDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures.StructuredLogsDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        serviceProvider.GetService<IDashboardWidgetRegistry>()?.Add(new DashboardWidgetDescriptor
        {
            Id = "diagnostics.structured-logs",
            Title = "Structured logs",
            ComponentType = typeof(StructuredLogsDashboardWidget),
            Zone = DashboardWidgetZone.Diagnostics,
            Order = 10,
            Span = DashboardWidgetSpan.Half,
            UsesTimeRange = true,
            RequiredRemoteFeatureName = RemoteFeatureName
        });

        return ValueTask.CompletedTask;
    }
}
