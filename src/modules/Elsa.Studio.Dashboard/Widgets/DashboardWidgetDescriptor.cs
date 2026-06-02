using Elsa.Studio.Dashboard.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Dashboard.Widgets;

public record DashboardWidgetDescriptor(
    string Id,
    string Zone,
    int Order,
    Type ComponentType,
    string? Title = null,
    string? RequiredBackendCapability = null,
    string? PayloadKind = null)
{
    public bool IsVisible(DashboardWidgetContext context) => context.Snapshot != null;
}

public record DashboardWidgetContext(
    string SelectedRange,
    bool Loading,
    DateTimeOffset? LastRefreshedAt,
    DashboardLoadStatus Status,
    string? Message,
    DashboardSnapshot? Snapshot,
    Func<Task> RefreshAsync,
    NavigationManager Navigation);

public static class DashboardWidgetZones
{
    public const string Metrics = "metrics";
    public const string Findings = "findings";
    public const string PrimaryPanels = "primary-panels";
    public const string SecondaryPanels = "secondary-panels";
    public const string DiagnosticsStatus = "diagnostics-status";
}
