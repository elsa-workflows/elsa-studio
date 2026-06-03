using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard;
using Elsa.Studio.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard;
using Elsa.Studio.Diagnostics.StructuredLogs.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Workflows.Dashboard;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardModuleRegistrationTests
{
    private readonly BackendApiConfig _backendApiConfig = new();

    [Fact]
    public async Task InitializeFeaturesAsync_WhenDashboardRemoteFeaturesDisabled_RegistersNoCompanionWidgets()
    {
        using var provider = BuildProvider([]);

        await InitializeFeaturesAsync(provider);

        var widgets = provider.GetRequiredService<IDashboardWidgetProvider>().GetWidgets();
        Assert.Empty(widgets);
    }

    [Fact]
    public async Task InitializeFeaturesAsync_WhenDashboardRemoteFeaturesEnabled_RegistersExpectedCompanionWidgets()
    {
        using var provider = BuildProvider(
            WorkflowDashboardFeature.RemoteFeatureName,
            ConsoleLogsDashboardFeature.RemoteFeatureName,
            StructuredLogsDashboardFeature.RemoteFeatureName);

        await InitializeFeaturesAsync(provider);

        var widgetIds = provider.GetRequiredService<IDashboardWidgetProvider>().GetWidgets().Select(x => x.Id).ToList();
        Assert.Equal(
            [
                "workflows.metrics",
                "workflows.needs-attention",
                "workflows.execution-trend",
                "workflows.recent-activity",
                "workflows.hotspots",
                "diagnostics.structured-logs",
                "diagnostics.console-logs"
            ],
            widgetIds);
    }

    [Fact]
    public async Task InitializeFeaturesAsync_WhenCompanionWidgetsRegister_DeclaresTimeRangeUsage()
    {
        using var provider = BuildProvider(
            WorkflowDashboardFeature.RemoteFeatureName,
            ConsoleLogsDashboardFeature.RemoteFeatureName,
            StructuredLogsDashboardFeature.RemoteFeatureName);

        await InitializeFeaturesAsync(provider);

        var widgets = provider.GetRequiredService<IDashboardWidgetProvider>().GetWidgets();
        var timeRangeWidgetIds = widgets.Where(x => x.UsesTimeRange).Select(x => x.Id).ToList();

        Assert.Equal(
            [
                "workflows.metrics",
                "workflows.needs-attention",
                "workflows.execution-trend",
                "workflows.recent-activity",
                "workflows.hotspots"
            ],
            timeRangeWidgetIds);
        Assert.False(widgets.Single(x => x.Id == "diagnostics.structured-logs").UsesTimeRange);
        Assert.False(widgets.Single(x => x.Id == "diagnostics.console-logs").UsesTimeRange);
    }

    [Fact]
    public async Task InitializeFeaturesAsync_WhenWidgetsAreReadBeforeRemoteFeaturesInitialize_RaisesEventAfterWidgetsRegister()
    {
        using var provider = BuildProvider(
            WorkflowDashboardFeature.RemoteFeatureName,
            ConsoleLogsDashboardFeature.RemoteFeatureName,
            StructuredLogsDashboardFeature.RemoteFeatureName);
        var widgetProvider = provider.GetRequiredService<IDashboardWidgetProvider>();
        var featureService = CreateFeatureService(provider);
        IReadOnlyList<DashboardWidgetDescriptor>? refreshedWidgets = null;

        Assert.Empty(widgetProvider.GetWidgets());

        featureService.Initialized += () => refreshedWidgets = widgetProvider.GetWidgets();
        await featureService.InitializeFeaturesAsync();

        Assert.NotNull(refreshedWidgets);
        Assert.Equal(7, refreshedWidgets.Count);
    }

    [Fact]
    public void DashboardModule_DoesNotReferenceWorkflowOrDiagnosticsModules()
    {
        var references = typeof(Feature).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Elsa.Studio.Workflows", references);
        Assert.DoesNotContain("Elsa.Studio.Diagnostics.ConsoleLogs", references);
        Assert.DoesNotContain("Elsa.Studio.Diagnostics.StructuredLogs", references);
    }

    private ServiceProvider BuildProvider(params string[] enabledRemoteFeatures)
    {
        var services = new ServiceCollection();
        services.AddDashboardModule();
        services.AddWorkflowsModule();
        services.AddConsoleLogsModule(_backendApiConfig);
        services.AddStructuredLogsModule(_backendApiConfig);
        services.AddSingleton<IRemoteFeatureProvider>(new TestRemoteFeatureProvider(enabledRemoteFeatures));

        return services.BuildServiceProvider();
    }

    private static async Task InitializeFeaturesAsync(ServiceProvider provider)
    {
        var featureService = CreateFeatureService(provider);
        await featureService.InitializeFeaturesAsync();
    }

    private static DefaultFeatureService CreateFeatureService(ServiceProvider provider) =>
        new(
            provider.GetRequiredService<IEnumerable<IFeature>>(),
            provider.GetRequiredService<IRemoteFeatureProvider>());

    private class TestRemoteFeatureProvider(params string[] enabledRemoteFeatures) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(enabledRemoteFeatures.Contains(featureName, StringComparer.Ordinal));
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(enabledRemoteFeatures.Select(ToDescriptor).AsEnumerable());
        }

        private static FeatureDescriptor ToDescriptor(string fullName)
        {
            var separatorIndex = fullName.LastIndexOf('.');
            var name = fullName[(separatorIndex + 1)..];
            var ns = fullName[..separatorIndex];

            return new FeatureDescriptor
            {
                Name = name,
                Namespace = ns,
                FullName = fullName,
                DisplayName = name,
                Description = string.Empty
            };
        }
    }
}
