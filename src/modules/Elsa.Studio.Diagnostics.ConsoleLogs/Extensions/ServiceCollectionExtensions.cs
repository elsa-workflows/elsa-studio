using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Client;
using Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Menu;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Dashboard;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Widgets;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Extensions;

/// <summary>
/// Contains extension methods for the console logs module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the diagnostics console logs module.
    /// </summary>
    public static IServiceCollection AddConsoleLogsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, ConsoleLogsMenu>()
            .AddScoped<IWidget, WorkflowInstanceConsoleLogsTabWidget>()
            .AddScoped<IWidget, WorkflowInstanceConsoleLogsLeftPanelTabWidget>()
            .AddDashboardWidget<ConsoleLogsDashboardWidget>("diagnostics.console-logs", DashboardWidgetZones.DiagnosticsStatus, 200, "Console logs", requiredBackendCapability: "ConsoleLogs", payloadKind: "Diagnostics.ConsoleLogs")
            .AddScoped<IConsoleLogService, RemoteConsoleLogService>()
            .AddTransient<IConsoleLogObserver, SignalRConsoleLogObserver>()
            .AddScoped<ConsoleLogExportFormatter>()
            .AddScoped<ConsoleLogUrlStateMapper>()
            .AddRemoteApi<IConsoleLogsApi>(backendApiConfig);
    }
}
