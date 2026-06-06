using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Pages;

public partial class Index : IAsyncDisposable
{
    private static readonly DashboardWidgetZone[] MainZones =
    [
        DashboardWidgetZone.Findings,
        DashboardWidgetZone.Primary,
        DashboardWidgetZone.Secondary,
        DashboardWidgetZone.Diagnostics
    ];

    private IReadOnlyList<DashboardWidgetDescriptor> _widgets = [];
    private string _selectedRange = DashboardRangeKeys.TwentyFourHours;
    private int _refreshVersion;
    private DateTimeOffset? _lastRefreshedAt;

    [Inject] private IDashboardWidgetProvider DashboardWidgetProvider { get; set; } = null!;

    private string BackendLabel => "Selected backend";

    private string LastRefreshedLabel => _lastRefreshedAt == null ? "Not refreshed yet" : $"Refreshed {DashboardMetricFormatter.RelativeTimestamp(_lastRefreshedAt)}";
    private string WidgetCountLabel => _widgets.Count == 1 ? "1 widget" : $"{_widgets.Count} widgets";
    private DashboardWidgetContext WidgetContext => new(_selectedRange, _refreshVersion);

    protected override void OnInitialized()
    {
        _widgets = DashboardWidgetProvider.GetWidgets();
        Refresh();
    }

    private async Task OnRangeChangedAsync(string? range)
    {
        _selectedRange = DashboardRangeMapper.Normalize(range);
        Refresh();
        await Task.CompletedTask;
    }

    private Task RefreshAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        _refreshVersion++;
        _lastRefreshedAt = DateTimeOffset.UtcNow;
    }

    private bool HasWidgets(DashboardWidgetZone zone) => _widgets.Any(x => x.Zone == zone);

    private RenderFragment RenderZone(DashboardWidgetZone zone) => builder =>
    {
        var sequence = 0;

        foreach (var widget in _widgets.Where(x => x.Zone == zone))
        {
            builder.OpenComponent<MudItem>(sequence++);
            builder.AddAttribute(sequence++, "xs", 12);
            builder.AddAttribute(sequence++, "sm", GetSmallColumns(widget));
            builder.AddAttribute(sequence++, "lg", GetLargeColumns(widget));
            builder.OpenComponent<DynamicComponent>(sequence++);
            builder.AddAttribute(sequence++, "Type", widget.ComponentType);
            builder.AddAttribute(sequence++, "Parameters", widget.Parameters);
            builder.CloseComponent();
            builder.CloseComponent();
        }
    };

    private static int GetSmallColumns(DashboardWidgetDescriptor widget) => widget.Span == DashboardWidgetSpan.Compact ? 6 : 12;

    private static int GetLargeColumns(DashboardWidgetDescriptor widget) => widget.Span switch
    {
        DashboardWidgetSpan.Compact => 4,
        DashboardWidgetSpan.Wide => 8,
        DashboardWidgetSpan.Full => 12,
        _ => widget.Zone is DashboardWidgetZone.Diagnostics or DashboardWidgetZone.Findings ? 4 : 8
    };

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
