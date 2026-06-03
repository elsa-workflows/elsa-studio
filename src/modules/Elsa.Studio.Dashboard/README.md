# Elsa.Studio.Dashboard

`Elsa.Studio.Dashboard` provides the dashboard shell and shared widget composition contracts. It does not own workflow, console log, or structured log data loading. Companion Studio modules register dashboard widgets only when their matching backend dashboard feature is enabled.

## Widget Composition

Dashboard widgets are contributed through `DashboardWidgetDescriptor`:

- `Id`: stable unique ID, for example `workflows.execution-trend`
- `ComponentType`: Blazor component rendered by the shell
- `Zone`: semantic placement, not a concrete grid coordinate
- `Order`: deterministic ordering within the zone
- `Title`: optional display name for tooling and diagnostics
- `Span`: optional size hint for compact, wide, or full-width widgets
- `RequiredRemoteFeatureName`: backend feature that enables the widget

Supported zones are:

- `Metrics`: top-level KPI bands and compact counters
- `Findings`: prioritized status and attention panels
- `Primary`: wide charts or primary operational panels
- `Secondary`: supporting tables and activity panels
- `Diagnostics`: status cards and health panels

## Remote-Gated Registration

Register widgets from a companion feature that is marked with `RemoteFeatureAttribute`. The default feature service initializes that feature only when the backend advertises the matching shell feature.

```csharp
[RemoteFeature(RemoteFeatureName)]
public class ExampleDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Example.Backend.ShellFeatures.ExampleDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        serviceProvider.GetService<IDashboardWidgetRegistry>()?.Add(new DashboardWidgetDescriptor
        {
            Id = "example.status",
            Title = "Example status",
            ComponentType = typeof(ExampleStatusDashboardWidget),
            Zone = DashboardWidgetZone.Diagnostics,
            Order = 10,
            RequiredRemoteFeatureName = RemoteFeatureName
        });

        return ValueTask.CompletedTask;
    }
}
```

The optional registry lookup keeps companion modules usable when a host does not install `Elsa.Studio.Dashboard`.

## Widget Data Loading

The shell supplies a cascading `DashboardWidgetContext` with the selected range and refresh version. Widgets should load only the backend APIs needed by that widget, cancel in-flight work when the context changes, and keep their own empty, loading, error, and unavailable states.

Use a scoped provider when multiple widgets from the same companion module share one backend snapshot. For example, workflow dashboard widgets share `IWorkflowDashboardDataProvider`, while console and structured log widgets load only dashboard overview data.

## Host Registration

Install the dashboard shell and the companion modules you want:

```csharp
services.AddDashboardModule(backendApiConfig);
services.AddWorkflowsModule();
services.AddConsoleLogsModule(backendApiConfig);
services.AddStructuredLogsModule(backendApiConfig);
```

The shell remains coherent when any companion is absent or when the backend does not advertise a companion dashboard feature.
