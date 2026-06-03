namespace Elsa.Studio.Dashboard.Models;

public record DashboardWidgetDescriptor
{
    public required string Id { get; init; }
    public required Type ComponentType { get; init; }
    public DashboardWidgetZone Zone { get; init; }
    public int Order { get; init; }
    public string? Title { get; init; }
    public DashboardWidgetSpan Span { get; init; } = DashboardWidgetSpan.Auto;
    public bool UsesTimeRange { get; init; }
    public string? RequiredRemoteFeatureName { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}

public enum DashboardWidgetZone
{
    Metrics,
    Findings,
    Primary,
    Secondary,
    Diagnostics
}

public enum DashboardWidgetSpan
{
    Auto,
    Compact,
    Wide,
    Full
}
