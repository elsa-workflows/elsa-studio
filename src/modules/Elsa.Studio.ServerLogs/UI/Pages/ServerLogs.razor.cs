using Elsa.Studio.ServerLogs.Contracts;
using Elsa.Studio.ServerLogs.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.ServerLogs.UI.Pages;

/// <summary>
/// Server logs page.
/// </summary>
[UsedImplicitly]
public partial class ServerLogs : IAsyncDisposable
{
    private const string LogSurfaceId = "server-logs-surface";
    private static readonly IReadOnlyCollection<ServerLogLevel> LogLevels =
    [
        ServerLogLevel.Trace,
        ServerLogLevel.Debug,
        ServerLogLevel.Information,
        ServerLogLevel.Warning,
        ServerLogLevel.Error,
        ServerLogLevel.Critical
    ];

    private readonly List<ServerLogEvent> _rows = new();
    private readonly List<ServerLogSource> _sources = new();
    private readonly HashSet<string> _selectedLogIds = new(StringComparer.Ordinal);
    private CancellationTokenSource _cancellationTokenSource = new();
    private IJSObjectReference? _scriptModule;
    private bool _queryInitialized;

    /// <summary>
    /// Gets or sets the server log service.
    /// </summary>
    [Inject] private IServerLogService ServerLogService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the live observer.
    /// </summary>
    [Inject] private IServerLogObserver Observer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service.
    /// </summary>
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JS runtime.
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets the current view state.
    /// </summary>
    protected ServerLogViewState ViewState { get; } = new();

    /// <summary>
    /// Gets the rendered rows.
    /// </summary>
    protected IReadOnlyList<ServerLogEvent> Rows => _rows;

    /// <summary>
    /// Gets the available backend log sources.
    /// </summary>
    protected IReadOnlyList<ServerLogSource> Sources => _sources;

    /// <summary>
    /// Gets a value indicating whether the page is loading.
    /// </summary>
    protected bool IsLoading { get; private set; }

    /// <summary>
    /// Gets the last error message.
    /// </summary>
    protected string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the upstream dropped event count.
    /// </summary>
    protected int BackendDroppedCount { get; private set; }

    protected string StatusText => ViewState.ConnectionStatus switch
    {
        ServerLogConnectionStatus.Connecting => "Connecting",
        ServerLogConnectionStatus.Connected => "Connected",
        ServerLogConnectionStatus.Reconnecting => "Reconnecting",
        ServerLogConnectionStatus.Unavailable => "Unavailable",
        ServerLogConnectionStatus.Unauthorized => "Unauthorized",
        _ => "Disconnected"
    };

    protected string StatusCssClass => $"server-logs-status-{ViewState.ConnectionStatus.ToString().ToLowerInvariant()}";
    protected string PauseIcon => ViewState.IsPaused ? Icons.Material.Filled.PlayArrow : Icons.Material.Filled.Pause;
    protected string PauseTooltip => ViewState.IsPaused ? "Resume" : "Pause";
    protected string LogSurfaceCssClass => $"server-logs-surface{(ViewState.WrapMessages ? " server-logs-wrap" : "")}{(ViewState.Compact ? " server-logs-compact" : "")}";
    protected bool HasSelection => _selectedLogIds.Count > 0;
    protected bool HasActiveFilter => !string.IsNullOrWhiteSpace(ViewState.Filter.SourceId) ||
                                      ViewState.Filter.MinimumLevel is { } level && level != ServerLogLevel.Information ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.Text) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.WorkflowInstanceId);
    protected string EmptyText => HasActiveFilter ? "No server logs match the current filters." : "No server logs received yet.";
    protected string? StateMessage => ViewState.ConnectionStatus switch
    {
        ServerLogConnectionStatus.Unauthorized => "You do not have permission to view server logs.",
        ServerLogConnectionStatus.Unavailable => "Server log streaming is not available on this backend.",
        ServerLogConnectionStatus.Disconnected when Rows.Count == 0 => "The live log stream is disconnected.",
        ServerLogConnectionStatus.Reconnecting => "Reconnecting to the live log stream.",
        _ => null
    };

    protected Severity StateSeverity => ViewState.ConnectionStatus switch
    {
        ServerLogConnectionStatus.Unauthorized => Severity.Error,
        ServerLogConnectionStatus.Unavailable => Severity.Warning,
        ServerLogConnectionStatus.Disconnected => Severity.Warning,
        _ => Severity.Info
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        ApplyQueryFromUrl();
        Observer.LogReceived += OnLogReceivedAsync;
        Observer.DroppedEventsReceived += OnDroppedEventsReceivedAsync;
        Observer.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        Observer.SourceChanged += OnSourceChangedAsync;
        await ActivateAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Observer.LogReceived -= OnLogReceivedAsync;
        Observer.DroppedEventsReceived -= OnDroppedEventsReceivedAsync;
        Observer.ConnectionStatusChanged -= OnConnectionStatusChangedAsync;
        Observer.SourceChanged -= OnSourceChangedAsync;

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        await Observer.DisposeAsync();

        if (_scriptModule != null)
            await _scriptModule.DisposeAsync();
    }

    protected async Task TogglePausedAsync()
    {
        ViewState.IsPaused = !ViewState.IsPaused;

        if (!ViewState.IsPaused)
            await ScrollToBottomAsync();
    }

    protected Task ClearAsync()
    {
        _rows.Clear();
        _selectedLogIds.Clear();
        ViewState.LocalDroppedRows = 0;
        BackendDroppedCount = 0;
        return InvokeAsync(StateHasChanged);
    }

    protected async Task ReconnectAsync()
    {
        ErrorMessage = null;
        await Observer.ReconnectAsync(ViewState.Filter, _cancellationTokenSource.Token);
    }

    protected async Task SetSourceAsync(string? sourceId)
    {
        ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId;
        await RefreshFilterAsync();
    }

    protected async Task SetMinimumLevelAsync(ServerLogLevel? level)
    {
        ViewState.Filter.MinimumLevel = level;
        await RefreshFilterAsync();
    }

    protected async Task SetTextFilterAsync(string? text)
    {
        ViewState.Filter.Text = string.IsNullOrWhiteSpace(text) ? null : text;
        await RefreshFilterAsync();
    }

    protected async Task SetAutoScrollAsync(bool value)
    {
        ViewState.AutoScroll = value;

        if (value)
            await ScrollToBottomAsync();
    }

    protected Task SetWrapMessagesAsync(bool value)
    {
        ViewState.WrapMessages = value;
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetCompactAsync(bool value)
    {
        ViewState.Compact = value;
        return InvokeAsync(StateHasChanged);
    }

    protected static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

    protected static string Shorten(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value ?? "";

        return string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 1)), "...");
    }

    protected static string LevelCssClass(ServerLogLevel level) => $"server-log-level server-log-level-{level.ToString().ToLowerInvariant()}";

    protected string RowCssClass(ServerLogEvent logEvent) => $"server-log-row server-log-row-{logEvent.Level.ToString().ToLowerInvariant()}{(IsSelected(logEvent) ? " server-log-row-selected" : "")}";

    protected string RowSelectionLabel(ServerLogEvent logEvent) => $"Select log row {FormatTimestamp(logEvent.Timestamp)} {logEvent.Level} {Shorten(logEvent.Category, 64)}";

    protected bool IsSelected(ServerLogEvent logEvent) => _selectedLogIds.Contains(logEvent.Id);

    protected Task SetSelectedAsync(ServerLogEvent logEvent, bool selected)
    {
        if (selected)
            _selectedLogIds.Add(logEvent.Id);
        else
            _selectedLogIds.Remove(logEvent.Id);

        return InvokeAsync(StateHasChanged);
    }

    protected async Task CopySelectedAsync()
    {
        var selectedRows = _rows.Where(x => _selectedLogIds.Contains(x.Id)).ToList();
        await CopyRowsAsync(selectedRows);
    }

    protected Task CopyVisibleAsync()
    {
        return CopyRowsAsync(_rows);
    }

    protected string SourceDisplayName(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return "";

        var source = _sources.FirstOrDefault(x => string.Equals(x.Id, sourceId, StringComparison.OrdinalIgnoreCase));
        return source == null ? sourceId : SourceDisplayName(source);
    }

    protected static string SourceDisplayName(ServerLogSource source)
    {
        var name = !string.IsNullOrWhiteSpace(source.DisplayName) ? source.DisplayName : source.Id;
        return source.Status == ServerLogSourceStatus.Connected ? name : $"{name} ({source.Status})";
    }

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadSourcesAsync(cancellationToken);
            var recent = await ServerLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, cancellationToken);
            BackendDroppedCount = recent.DroppedEventCount;

            foreach (var logEvent in recent.Items.OrderBy(x => x.Timestamp))
                AddRow(logEvent);

            await Observer.StartAsync(ViewState.Filter, cancellationToken);
            await ScrollToBottomAsync();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshFilterAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        UpdateUrlFromFilter();

        try
        {
            _rows.Clear();
            _selectedLogIds.Clear();
            ViewState.LocalDroppedRows = 0;
            BackendDroppedCount = 0;

            var recent = await ServerLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, _cancellationTokenSource.Token);
            BackendDroppedCount = recent.DroppedEventCount;

            foreach (var logEvent in recent.Items.OrderBy(x => x.Timestamp))
                AddRow(logEvent);

            await Observer.UpdateFilterAsync(ViewState.Filter, _cancellationTokenSource.Token);
            await ScrollToBottomAsync();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await ServerLogService.ListSourcesAsync(cancellationToken);
        _sources.Clear();
        _sources.AddRange(sources.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase));
    }

    private async Task OnLogReceivedAsync(ServerLogEvent logEvent)
    {
        if (ViewState.IsPaused)
            return;

        await InvokeAsync(async () =>
        {
            AddRow(logEvent);
            StateHasChanged();

            if (ViewState.AutoScroll)
                await ScrollToBottomAsync();
        });
    }

    private Task OnDroppedEventsReceivedAsync(ServerLogDroppedEventSummary summary)
    {
        BackendDroppedCount += summary.Count;
        return InvokeAsync(StateHasChanged);
    }

    private Task OnConnectionStatusChangedAsync(ServerLogConnectionStatus status)
    {
        ViewState.ConnectionStatus = status;
        return InvokeAsync(StateHasChanged);
    }

    private Task OnSourceChangedAsync(ServerLogSource source)
    {
        var index = _sources.FindIndex(x => string.Equals(x.Id, source.Id, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            _sources[index] = source;
        else
            _sources.Add(source);

        _sources.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
        return InvokeAsync(StateHasChanged);
    }

    private void AddRow(ServerLogEvent logEvent)
    {
        _rows.Add(logEvent);

        while (_rows.Count > ViewState.VisibleRowCap)
        {
            _selectedLogIds.Remove(_rows[0].Id);
            _rows.RemoveAt(0);
            ViewState.LocalDroppedRows++;
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            var module = await GetScriptModuleAsync();
            await module.InvokeVoidAsync("scrollToBottomById", LogSurfaceId);
        }
        catch (JSException)
        {
            // Scrolling is best-effort and should not interrupt log streaming.
        }
    }

    private void ApplyQueryFromUrl()
    {
        if (_queryInitialized)
            return;

        var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);

        if (query.TryGetValue("sourceId", out var sourceId))
            ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId.ToString();

        if (query.TryGetValue("level", out var level) && Enum.TryParse<ServerLogLevel>(level, true, out var parsedLevel))
            ViewState.Filter.MinimumLevel = parsedLevel;

        if (query.TryGetValue("text", out var text))
            ViewState.Filter.Text = string.IsNullOrWhiteSpace(text) ? null : text.ToString();

        if (query.TryGetValue("workflowInstanceId", out var workflowInstanceId))
            ViewState.Filter.WorkflowInstanceId = string.IsNullOrWhiteSpace(workflowInstanceId) ? null : workflowInstanceId.ToString();

        _queryInitialized = true;
    }

    private void UpdateUrlFromFilter()
    {
        if (!_queryInitialized)
            return;

        var parameters = new Dictionary<string, object?>
        {
            ["sourceId"] = ViewState.Filter.SourceId,
            ["level"] = ViewState.Filter.MinimumLevel?.ToString(),
            ["text"] = ViewState.Filter.Text,
            ["workflowInstanceId"] = ViewState.Filter.WorkflowInstanceId
        };
        var uri = NavigationManager.GetUriWithQueryParameters(parameters);

        if (!string.Equals(uri, NavigationManager.Uri, StringComparison.Ordinal))
            NavigationManager.NavigateTo(uri, replace: true);
    }

    private async Task CopyRowsAsync(IReadOnlyCollection<ServerLogEvent> rows)
    {
        if (rows.Count == 0)
            return;

        var text = string.Join(Environment.NewLine, rows.Select(FormatCopyLine));

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
            Snackbar.Add($"{rows.Count} log row(s) copied.", Severity.Success);
        }
        catch (JSException)
        {
            Snackbar.Add("Unable to copy log rows. Check browser clipboard permissions.", Severity.Warning);
        }
    }

    private string FormatCopyLine(ServerLogEvent logEvent)
    {
        return $"{logEvent.Timestamp:O}\t{logEvent.Level}\t{SourceDisplayName(logEvent.SourceId)}\t{logEvent.Category}\t{logEvent.Message}";
    }

    private async Task<IJSObjectReference> GetScriptModuleAsync()
    {
        _scriptModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.ServerLogs/serverLogs.js");
        return _scriptModule;
    }
}
