using Elsa.Studio.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardWidgetRegistrationTests
{
    [Fact]
    public void AddDashboardWidget_RegistersDescriptor()
    {
        var services = new ServiceCollection();

        services.AddDashboardWidget<TestWidget>("test", DashboardWidgetZones.PrimaryPanels, 20, "Test", "Capability", "Payload");
        var descriptor = services.BuildServiceProvider().GetRequiredService<DashboardWidgetDescriptor>();

        Assert.Equal("test", descriptor.Id);
        Assert.Equal(DashboardWidgetZones.PrimaryPanels, descriptor.Zone);
        Assert.Equal(20, descriptor.Order);
        Assert.Equal(typeof(TestWidget), descriptor.ComponentType);
        Assert.Equal("Capability", descriptor.RequiredBackendCapability);
        Assert.Equal("Payload", descriptor.PayloadKind);
    }

    [Fact]
    public void Descriptors_OrderDeterministicallyByOrderThenId()
    {
        var descriptors = new[]
        {
            new DashboardWidgetDescriptor("b", DashboardWidgetZones.Metrics, 10, typeof(TestWidget)),
            new DashboardWidgetDescriptor("a", DashboardWidgetZones.Metrics, 10, typeof(TestWidget)),
            new DashboardWidgetDescriptor("c", DashboardWidgetZones.Metrics, 5, typeof(TestWidget))
        };

        var ordered = descriptors.OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id).ToList();

        Assert.Equal(["c", "a", "b"], ordered);
    }

    [Fact]
    public void DashboardProject_DoesNotReferenceDiagnosticsModules()
    {
        var projectFile = FindRepositoryRoot().Combine("src/modules/Elsa.Studio.Dashboard/Elsa.Studio.Dashboard.csproj");
        var project = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Elsa.Studio.Diagnostics", project);
    }

    private sealed class TestWidget : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.Studio.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static string Combine(this DirectoryInfo directory, string path) => Path.Combine(directory.FullName, path);
}
