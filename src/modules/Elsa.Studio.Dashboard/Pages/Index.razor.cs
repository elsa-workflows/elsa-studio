using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

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
    private bool _disposed;
    private bool _subscribedToFeatureInitialized;

    [Inject] private IDashboardWidgetProvider DashboardWidgetProvider { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;

    private string BackendLabel => "Selected backend";

    private string LastRefreshedLabel => _lastRefreshedAt == null ? "Not refreshed yet" : $"Refreshed {DashboardMetricFormatter.RelativeTimestamp(_lastRefreshedAt)}";
    private string WidgetCountLabel => _widgets.Count == 1 ? "1 widget" : $"{_widgets.Count} widgets";
    private DashboardWidgetContext WidgetContext => new(_selectedRange, _refreshVersion);
    private bool HasTimeRangeWidgets => _widgets.Any(x => x.UsesTimeRange);

    protected override void OnInitialized()
    {
        LoadWidgets();
        Refresh();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _disposed)
            return;

        FeatureService.Initialized += OnFeatureServiceInitialized;
        _subscribedToFeatureInitialized = true;

        if (LoadWidgets())
            await InvokeAsync(StateHasChanged);
    }

    private void OnFeatureServiceInitialized()
    {
        _ = RefreshWidgetsAfterFeatureInitializationAsync();
    }

    private async Task RefreshWidgetsAfterFeatureInitializationAsync()
    {
        if (_disposed)
            return;

        try
        {
            await InvokeAsync(() =>
            {
                if (_disposed || !LoadWidgets())
                    return;

                StateHasChanged();
            });
        }
        catch (InvalidOperationException) when (_disposed)
        {
        }
    }

    private bool LoadWidgets()
    {
        var widgets = DashboardWidgetProvider.GetWidgets();

        if (_widgets.Select(x => x.Id).SequenceEqual(widgets.Select(x => x.Id), StringComparer.Ordinal))
            return false;

        _widgets = widgets;
        return true;
    }

    private Task RefreshAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    private Task OnRangeChangedAsync(string? range)
    {
        var selectedRange = DashboardRangeMapper.Normalize(range);

        if (_selectedRange == selectedRange)
            return Task.CompletedTask;

        _selectedRange = selectedRange;
        Refresh();
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        _refreshVersion++;
        _lastRefreshedAt = DateTimeOffset.UtcNow;
    }

    private bool HasWidgets(DashboardWidgetZone zone) => _widgets.Any(x => x.Zone == zone);

    private IEnumerable<DashboardWidgetDescriptor> WidgetsFor(DashboardWidgetZone zone) => _widgets.Where(x => x.Zone == zone);

    private static IDictionary<string, object> GetParameters(DashboardWidgetDescriptor widget) =>
        widget.Parameters.ToDictionary(x => x.Key, x => x.Value!);

    private static int GetSmallColumns(DashboardWidgetDescriptor widget) => widget.Span == DashboardWidgetSpan.Compact ? 6 : 12;

    private static int GetLargeColumns(DashboardWidgetDescriptor widget) => widget.Span switch
    {
        DashboardWidgetSpan.Compact => 4,
        DashboardWidgetSpan.Wide => 8,
        DashboardWidgetSpan.Full => 12,
        _ => widget.Zone is DashboardWidgetZone.Diagnostics or DashboardWidgetZone.Findings ? 4 : 8
    };

    public ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_subscribedToFeatureInitialized)
            FeatureService.Initialized -= OnFeatureServiceInitialized;

        return ValueTask.CompletedTask;
    }
}
