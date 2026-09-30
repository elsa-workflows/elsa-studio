using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Pages;

public partial class Index : IAsyncDisposable
{
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DataScope? _loadedScope;
    private DashboardSnapshot? _snapshot;
    private DashboardLoadStatus _status = DashboardLoadStatus.Unavailable;
    private string _selectedRange = DashboardRangeKeys.TwentyFourHours;
    private string? _message;
    private bool _loading;
    private bool _disposed;
    private DateTimeOffset? _lastRefreshedAt;

    [Inject] private IDashboardService DashboardService { get; set; } = null!;
    [Inject] private IDashboardWidgetRegistry WidgetRegistry { get; set; } = null!;
    [Inject] private IEnumerable<DashboardWidgetDescriptor> Widgets { get; set; } = [];
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;

    /// <summary>The user's permissions, cascaded by the shell's page guard.</summary>
    [CascadingParameter] private UserPermissions Permissions { get; set; } = UserPermissions.Unknown;

    private DashboardWidgetContext WidgetContext => new(
        _selectedRange,
        _loading,
        _lastRefreshedAt,
        _status,
        _message,
        _snapshot,
        RefreshAsync,
        NavigationManager);

    private string BackendLabel
    {
        get
        {
            if (_snapshot == null)
                return "Selected backend";

            var overview = _snapshot.Overview;
            var backendName = string.IsNullOrWhiteSpace(overview.BackendName) ? "Backend" : overview.BackendName;
            return string.IsNullOrWhiteSpace(overview.EnvironmentName) ? backendName : $"{backendName} / {overview.EnvironmentName}";
        }
    }

    private string LastRefreshedLabel => _lastRefreshedAt == null ? "Not refreshed yet" : $"Refreshed {DashboardMetricFormatter.RelativeTimestamp(_lastRefreshedAt)}";

    private bool IsLoadingFirstSnapshot => _snapshot == null && (_loading || AwaitingWidgets);

    // Until the features are initialized, the widgets they contribute may still be on their way.
    private bool AwaitingWidgets => PermittedWidgets.Count == 0 && !FeatureService.IsInitialized;

    private bool ShowWelcome => PermittedWidgets.Count == 0 && FeatureService.IsInitialized;

    private bool ShowRuntime =>
        _snapshot is { } snapshot
        && !snapshot.Overview.Runtime.Capability.IsUnauthorized
        && Permissions.HasAny(DashboardPermissions.ForData(DashboardPermissions.WorkflowRuntime));

    private string StatusLabel => _status switch
    {
        _ when IsLoadingFirstSnapshot => "Loading dashboard",
        DashboardLoadStatus.Unauthorized => "No access",
        DashboardLoadStatus.BackendDisconnected => "Backend disconnected",
        DashboardLoadStatus.Failed => "Refresh failed",
        DashboardLoadStatus.Loaded => "Loaded",
        _ => "Dashboard unavailable"
    };

    private Color StatusColor => IsLoadingFirstSnapshot ? Color.Default : Color.Error;

    private string StatusIcon => IsLoadingFirstSnapshot ? Icons.Material.Outlined.HourglassEmpty : Icons.Material.Outlined.CloudOff;

    private Severity AlertSeverity => _status switch
    {
        DashboardLoadStatus.Unauthorized => Severity.Warning,
        DashboardLoadStatus.Loaded => Severity.Info,
        _ => Severity.Error
    };

    private IReadOnlyCollection<DashboardWidgetDescriptor> PermittedWidgets =>
        Widgets
            .Concat(WidgetRegistry.List())
            .DistinctBy(x => x.Id)
            .Where(x => x.IsPermitted(Permissions))
            .ToList();

    // The data the permitted widgets need, limited to what the dashboard API serves this user: nothing without a widget,
    // and the workflow instance data behind trends, activity, findings and hotspots only to users who may read it.
    private DataScope RequiredScope =>
        PermittedWidgets.Count == 0 ? DataScope.None
        : Permissions.HasAny(DashboardPermissions.ForData(DashboardPermissions.WorkflowInstances)) ? DataScope.Everything
        : DataScope.Overview;

    private IReadOnlyCollection<DashboardWidgetDescriptor> GetWidgets(string zone) =>
        PermittedWidgets
            .Where(x => x.Zone == zone && x.IsVisible(WidgetContext))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

    // Subscribed before the first render, so widgets registered while it is on its way are not missed.
    protected override void OnInitialized() => FeatureService.Initialized += OnFeatureServiceInitialized;

    // Loads on the first render, and again when a change in the user's permissions changes the data the page needs.
    protected override Task OnParametersSetAsync() => LoadIfScopeChangedAsync();

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
            await InvokeAsync(async () =>
            {
                var loading = LoadIfScopeChangedAsync();
                StateHasChanged();
                await loading;
                StateHasChanged();
            });
        }
        catch (InvalidOperationException) when (_disposed)
        {
        }
    }

    private async Task LoadIfScopeChangedAsync()
    {
        if (RequiredScope != _loadedScope)
            await RefreshAsync();
    }

    private async Task OnRangeChangedAsync(string? range)
    {
        var selectedRange = DashboardRangeMapper.Normalize(range);

        if (_selectedRange == selectedRange)
            return;

        _selectedRange = selectedRange;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadAsync(_selectedRange);
    }

    private async Task LoadAsync(string range)
    {
        var scope = RequiredScope;
        _loadedScope = scope;

        await CancelCurrentLoadAsync();

        // Without a widget to show there is nothing to load, and the user may not be allowed to load it anyway.
        if (scope == DataScope.None)
        {
            _loading = false;
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _loadCancellationTokenSource = cancellationTokenSource;
        _loading = true;
        _message = null;

        try
        {
            var result = scope == DataScope.Everything
                ? await DashboardService.LoadAsync(range, cancellationToken: cancellationTokenSource.Token)
                : DashboardLoadResult.FromOverview(await DashboardService.LoadOverviewAsync(range, cancellationToken: cancellationTokenSource.Token));
            _status = result.Status;

            if (result.Snapshot != null)
            {
                _snapshot = result.Snapshot;
                _lastRefreshedAt = DateTimeOffset.UtcNow;
                _message = null;
            }
            else
            {
                _message = result.Message;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            _status = DashboardLoadStatus.Failed;
            _message = e.Message;
        }
        finally
        {
            if (ReferenceEquals(_loadCancellationTokenSource, cancellationTokenSource))
            {
                _loading = false;
                _loadCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private async Task CancelCurrentLoadAsync()
    {
        if (_loadCancellationTokenSource == null)
            return;

        await _loadCancellationTokenSource.CancelAsync();
        _loadCancellationTokenSource = null;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        FeatureService.Initialized -= OnFeatureServiceInitialized;
        await CancelCurrentLoadAsync();
    }

    private enum DataScope
    {
        None,
        Overview,
        Everything
    }
}
