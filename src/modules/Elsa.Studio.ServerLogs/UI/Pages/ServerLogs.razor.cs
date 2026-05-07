using Elsa.Studio.ServerLogs.Contracts;
using Elsa.Studio.ServerLogs.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
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
    private readonly List<ServerLogEvent> _rows = new();
    private CancellationTokenSource _cancellationTokenSource = new();

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
    /// Gets the current view state.
    /// </summary>
    protected ServerLogViewState ViewState { get; } = new();

    /// <summary>
    /// Gets the rendered rows.
    /// </summary>
    protected IReadOnlyList<ServerLogEvent> Rows => _rows;

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

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Observer.LogReceived += OnLogReceivedAsync;
        Observer.DroppedEventsReceived += OnDroppedEventsReceivedAsync;
        Observer.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        await ActivateAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        await Observer.DisposeAsync();
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
        ViewState.LocalDroppedRows = 0;
        BackendDroppedCount = 0;
        return InvokeAsync(StateHasChanged);
    }

    protected async Task ReconnectAsync()
    {
        ErrorMessage = null;
        await Observer.ReconnectAsync(ViewState.Filter, _cancellationTokenSource.Token);
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

    protected string RowCssClass(ServerLogEvent logEvent) => $"server-log-row server-log-row-{logEvent.Level.ToString().ToLowerInvariant()}";

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
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

    private void AddRow(ServerLogEvent logEvent)
    {
        _rows.Add(logEvent);

        while (_rows.Count > ViewState.VisibleRowCap)
        {
            _rows.RemoveAt(0);
            ViewState.LocalDroppedRows++;
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("eval", $"document.getElementById('{LogSurfaceId}')?.scrollTo({{ top: document.getElementById('{LogSurfaceId}').scrollHeight }})");
        }
        catch (JSException)
        {
            // Scrolling is best-effort and should not interrupt log streaming.
        }
    }
}
