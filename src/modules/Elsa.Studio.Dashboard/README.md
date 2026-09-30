# Elsa.Studio.Dashboard

`Elsa.Studio.Dashboard` provides the dashboard shell and shared widget composition contracts. It does not own workflow, console log, structured log, or OpenTelemetry data loading. Companion Studio modules register dashboard widgets only when their matching backend feature is enabled.

## Widget Composition

Dashboard widgets are contributed through `DashboardWidgetDescriptor`:

- `Id`: stable unique ID, for example `dashboard.workflow.trend`
- `Zone`: semantic placement, not a concrete grid coordinate
- `Order`: deterministic ordering within the zone
- `ComponentType`: Blazor component rendered by the shell
- `Title`: optional display name for tooling and diagnostics
- `RequiredBackendCapability`: backend capability needed by the widget
- `PayloadKind`: optional snapshot payload key used by the widget
- `RequiredPermissions` (init-only): the permissions that let a user see the widget; see [Permissions](#permissions)

Supported zones are:

- `DashboardWidgetZones.Metrics`: top-level KPI bands and compact counters
- `DashboardWidgetZones.Trend`: full-width execution charts and time-series panels
- `DashboardWidgetZones.Activity`: primary activity tables in the 8/12 operational column
- `DashboardWidgetZones.Findings`: prioritized status and attention panels
- `DashboardWidgetZones.SecondaryPanels`: supporting panels in the 4/12 operational column
- `DashboardWidgetZones.DiagnosticsStatus`: diagnostics status cards in the equal-width diagnostics row

`PrimaryPanels` is retained as a legacy zone and is rendered with the trend row so independently deployed companion modules remain visible. New widgets should use `Trend`, `Activity`, `Findings`, `SecondaryPanels`, or `DiagnosticsStatus` according to their operational role.

## Remote-Gated Registration

Register widgets from a companion feature that is marked with `RemoteFeatureAttribute`. The default feature service initializes that feature only when the backend advertises the matching shell feature.

```csharp
[RemoteFeature(RemoteFeatureName)]
public class ExampleDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Example.Backend.ShellFeatures.ExampleDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        serviceProvider.GetService<IDashboardWidgetRegistry>()?.Add(new(
            "example.status",
            DashboardWidgetZones.DiagnosticsStatus,
            10,
            typeof(ExampleStatusDashboardWidget),
            "Example status",
            RequiredBackendCapability: "Example",
            PayloadKind: "Example.Status")
        {
            RequiredPermissions = [new("example/status", PermissionVerbs.View)]
        });

        return ValueTask.CompletedTask;
    }
}
```

The optional registry lookup keeps companion modules usable when a host does not install `Elsa.Studio.Dashboard`.

Widgets registered through DI declare their permissions with the `requiredPermissions` parameter of `AddDashboardWidget<TComponent>()`.

## Permissions

Every signed-in user can open the dashboard: the page (`/`) and its menu item require no permission. Each widget is shown only to the users permitted to see its data. A widget the user may not see is hidden, not shown disabled.

- A widget is shown to users holding **any** of its `RequiredPermissions`. A widget that declares none is shown to every user.
- Declare the view permission of the data the widget shows. When that data comes from the dashboard API, use `DashboardPermissions.ForData(resource)`: the API serves each section to `dashboard:view` as well as to the view permission of its data, so the widget should be visible with either. When the widget loads its data from another API, declare only that API's permission.
- When the user's permissions are unknown (their sign-in carries no Elsa permissions), every widget is shown, as everywhere else in Studio.
- The dashboard requests only the data its visible widgets need, from endpoints the user may call: nothing when no widget is visible, the overview for any widget, and the workflow instance endpoints (trends, recent activity, needs attention, hotspots) only for users holding `dashboard:view` or `workflows/instances:view`.
- The backend marks an overview section the user may not read with `Capability` `Unauthorized` and sends no data for it. A widget should leave such a section out (check `Capability.IsUnauthorized` and render nothing) rather than report it: the user is not meant to see it.
- A user who may see no widget gets a welcome panel with shortcuts to the pages they can open, in navigation order. When there is none, the dashboard shows the "No pages are available for your role" notice instead.

The built-in widgets declare:

| Widget | Shown with |
| --- | --- |
| Workflow metrics, needs attention, trends, recent activity, hotspots | `dashboard:view` or `workflows/instances:view` |
| Structured logs | `dashboard:view` or `diagnostics/structured-logs:view` |
| Console logs | `dashboard:view` or `diagnostics/console-logs:view` |
| OpenTelemetry | `diagnostics/opentelemetry:view` (its figures come from the OpenTelemetry API) |

The runtime status chip in the dashboard header shows with `dashboard:view` or `workflows/runtime:view`.

## Widget Data Loading

The shell supplies a `DashboardWidgetContext` parameter with the selected range, load status, latest dashboard snapshot, refresh callback, and navigation manager. Widgets should read the snapshot payloads they need and keep their own empty and unavailable states.

Use `PayloadKind` to document which payload a widget consumes. For example, workflow dashboard widgets consume `WorkflowInstances`, `WorkflowTrends`, `RecentActivity`, and `WorkflowHotspots`; log widgets consume their matching dashboard snapshot payloads; and the OpenTelemetry companion owns loading `OpenTelemetry.StorageDiagnostics`.

## Host Registration

Install the dashboard shell and the companion modules you want:

```csharp
services.AddDashboardModule(backendApiConfig);
services.AddWorkflowsModule(backendApiConfig);
services.AddWorkflowsDashboardModule();
services.AddConsoleLogsModule(backendApiConfig);
services.AddConsoleLogsDashboardModule();
services.AddStructuredLogsModule(backendApiConfig);
services.AddStructuredLogsDashboardModule();
services.AddOpenTelemetryDiagnosticsModule(backendApiConfig);
services.AddOpenTelemetryDashboardModule();
```

The shell remains coherent when any companion is absent or when the backend does not advertise a companion dashboard feature.
