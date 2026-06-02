using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class ConsoleLogsDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures.ConsoleLogsDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        serviceProvider.GetService<IDashboardWidgetRegistry>()?.Add(new DashboardWidgetDescriptor
        {
            Id = "diagnostics.console-logs",
            Title = "Console logs",
            ComponentType = typeof(ConsoleLogsDashboardWidget),
            Zone = DashboardWidgetZone.Diagnostics,
            Order = 20,
            RequiredRemoteFeatureName = RemoteFeatureName
        });

        return ValueTask.CompletedTask;
    }
}
