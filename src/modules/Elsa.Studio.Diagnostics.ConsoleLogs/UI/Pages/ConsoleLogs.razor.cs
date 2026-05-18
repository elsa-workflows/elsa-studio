using Elsa.Studio.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Refit;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.UI.Pages;

/// <summary>
/// Diagnostics console logs page.
/// </summary>
[UsedImplicitly]
public partial class ConsoleLogs : IAsyncDisposable
{
    private const string LogSurfaceId = "console-logs-surface";
    private readonly List<ConsoleLogSource> _sources = new();
    private readonly List<ConsoleLogLine> _refreshBufferedLines = new();
    private readonly ConditionalWeakTable<ConsoleLogLine, StrippedConsoleLogText> _strippedTextCache = new();
    private CancellationTokenSource _cancellationTokenSource = new();
    private IJSObjectReference? _scriptModule;
    private string? _fromFilterText;
    private string? _toFilterText;
    private bool _queryInitialized;
    private bool _isRefreshing;
    private long _refreshVersion;

    [Inject] private IConsoleLogService ConsoleLogService { get; set; } = default!;
    [Inject] private IConsoleLogObserver Observer { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ConsoleLogExportFormatter ExportFormatter { get; set; } = default!;
    [Inject] private ConsoleLogUrlStateMapper UrlStateMapper { get; set; } = default!;
    [Inject] private ConsoleLogTextHighlighter TextHighlighter { get; set; } = default!;

    protected ConsoleLogViewState ViewState { get; } = new();
    protected IReadOnlyCollection<ConsoleLogLine> Rows => ViewState.VisibleRows;
    protected IReadOnlyList<ConsoleLogSource> Sources => _sources;
    protected bool IsLoading { get; private set; }
    protected string? ErrorMessage { get; private set; }
    protected long BackendDroppedCount { get; private set; }
    protected string? FromFilterText => _fromFilterText;
    protected string? ToFilterText => _toFilterText;
    protected string StreamSelection => ConsoleLogUrlStateMapper.FormatStreams(ViewState.Filter.Streams);
    protected string StatusText => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Connecting => "Connecting",
        ConsoleLogConnectionStatus.Connected => ViewState.IsPaused ? "Paused" : "Connected",
        ConsoleLogConnectionStatus.Reconnecting => "Reconnecting",
        ConsoleLogConnectionStatus.Unavailable => "Unavailable",
        ConsoleLogConnectionStatus.Unauthorized => "Unauthorized",
        ConsoleLogConnectionStatus.Error => "Error",
        _ => "Disconnected"
    };
    protected string StatusCssClass => $"console-logs-status-{ViewState.ConnectionStatus.ToString().ToLowerInvariant()}{(ViewState.IsPaused ? " console-logs-status-paused" : "")}";
    protected string PauseIcon => ViewState.IsPaused ? Icons.Material.Filled.PlayArrow : Icons.Material.Filled.Pause;
    protected string PauseTooltip => ViewState.IsPaused ? "Resume" : "Pause";
    protected string LogSurfaceCssClass => $"console-logs-surface{(ViewState.Wrap ? " console-logs-wrap" : "")}{(ViewState.Compact ? " console-logs-compact" : "")}";
    protected bool HasActiveFilter => !string.IsNullOrWhiteSpace(ViewState.Filter.SourceId) ||
                                      !string.Equals(StreamSelection, "both", StringComparison.Ordinal) ||
                                      !string.IsNullOrWhiteSpace(ViewState.Filter.Text) ||
                                      ViewState.Filter.From != null ||
                                      ViewState.Filter.To != null;
    protected string EmptyText => HasActiveFilter ? "No console lines match the current filters." : "No console lines received yet.";
    protected string? StateMessage => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Unauthorized => "You do not have permission to view diagnostics console logs.",
        ConsoleLogConnectionStatus.Unavailable => "Diagnostics console logs are not available on this backend.",
        ConsoleLogConnectionStatus.Error => "The diagnostics console stream encountered an error.",
        ConsoleLogConnectionStatus.Disconnected when Rows.Count == 0 => "The live diagnostics console stream is disconnected.",
        ConsoleLogConnectionStatus.Reconnecting => "Reconnecting to the live diagnostics console stream.",
        _ => null
    };
    protected Severity StateSeverity => ViewState.ConnectionStatus switch
    {
        ConsoleLogConnectionStatus.Unauthorized => Severity.Error,
        ConsoleLogConnectionStatus.Error => Severity.Error,
        ConsoleLogConnectionStatus.Unavailable => Severity.Warning,
        ConsoleLogConnectionStatus.Disconnected => Severity.Warning,
        _ => Severity.Info
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        ApplyQueryFromUrl();
        Observer.LineReceived += OnLineReceivedAsync;
        Observer.DroppedLinesReceived += OnDroppedLinesReceivedAsync;
        Observer.ConnectionStatusChanged += OnConnectionStatusChangedAsync;
        Observer.SourceChanged += OnSourceChangedAsync;
        await ActivateAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Observer.LineReceived -= OnLineReceivedAsync;
        Observer.DroppedLinesReceived -= OnDroppedLinesReceivedAsync;
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
        {
            ViewState.FlushPendingRows();

            if (ViewState.FollowTail)
                await ScrollToBottomAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    protected Task ClearAsync()
    {
        ViewState.ClearVisibleRows();
        BackendDroppedCount = 0;
        return InvokeAsync(StateHasChanged);
    }

    protected async Task ReconnectAsync()
    {
        ErrorMessage = null;
        await Observer.ReconnectAsync(ViewState.Filter, _cancellationTokenSource.Token);
    }

    protected async Task CopyVisibleAsync()
    {
        await CopyTextAsync(ExportFormatter.FormatVisibleRows(Rows), $"{Rows.Count} console row(s) copied.");
    }

    protected async Task ExportVisibleAsync()
    {
        var text = ExportFormatter.FormatVisibleRows(Rows);

        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            var module = await GetScriptModuleAsync();
            await module.InvokeVoidAsync("downloadTextFile", CreateExportFileName(DateTimeOffset.Now), text, "text/tab-separated-values;charset=utf-8");
            Snackbar.Add($"{Rows.Count} console row(s) exported.", Severity.Success);
        }
        catch (JSException)
        {
            Snackbar.Add("Unable to export. Check browser download permissions.", Severity.Warning);
        }
    }

    protected async Task SetSourceAsync(string? sourceId)
    {
        if (IsLoading)
            return;

        ViewState.Filter.SourceId = string.IsNullOrWhiteSpace(sourceId) ? null : sourceId;
        await RefreshFilterAsync();
    }

    protected async Task SetStreamAsync(string stream)
    {
        if (IsLoading)
            return;

        ViewState.Filter.Streams = ConsoleLogUrlStateMapper.ParseStreams(stream);
        await RefreshFilterAsync();
    }

    protected async Task SetTextFilterAsync(string? text)
    {
        if (IsLoading)
            return;

        ViewState.Filter.Text = NormalizeFilterText(text);
        await RefreshFilterAsync();
    }

    protected async Task SetFromAsync(string? value)
    {
        if (IsLoading)
            return;

        _fromFilterText = NormalizeFilterText(value);
        ViewState.Filter.From = TryParseDateTimeOffset(_fromFilterText, out var parsed) ? parsed : null;
        await RefreshFilterAsync();
    }

    protected async Task SetToAsync(string? value)
    {
        if (IsLoading)
            return;

        _toFilterText = NormalizeFilterText(value);
        ViewState.Filter.To = TryParseDateTimeOffset(_toFilterText, out var parsed) ? parsed : null;
        await RefreshFilterAsync();
    }

    protected async Task SetFollowTailAsync(bool value)
    {
        ViewState.FollowTail = value;
        UpdateUrlFromState();

        if (value)
            await ScrollToBottomAsync();
    }

    protected Task SetWrapAsync(bool value)
    {
        ViewState.Wrap = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetCompactAsync(bool value)
    {
        ViewState.Compact = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected Task SetAnsiAsync(bool value)
    {
        ViewState.Ansi = value;
        UpdateUrlFromState();
        return InvokeAsync(StateHasChanged);
    }

    protected static string FormatTimestamp(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
    protected static string CreateExportFileName(DateTimeOffset timestamp) => $"diagnostics-console-logs-{timestamp:yyyyMMdd-HHmmss}.tsv";
    protected static string StreamCssClass(ConsoleLogStream stream) => $"console-log-stream console-log-stream-{ConsoleLogExportFormatter.StreamLabel(stream)}";
    protected string RowCssClass(ConsoleLogLine line) => $"console-log-row console-log-row-{ConsoleLogExportFormatter.StreamLabel(line.Stream)}";
    protected string DisplayText(ConsoleLogLine line) => ViewState.Ansi ? line.Text : GetStrippedText(line);
    protected static string Shorten(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) || value.Length <= maxLength ? value ?? "" : string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 1)), "...");
    protected static string SourceDisplayName(ConsoleLogSource source) => ConsoleLogExportFormatter.SourceDisplayName(source);

    protected static string SourceTitle(ConsoleLogSource source)
    {
        var builder = new StringBuilder(source.Id);

        foreach (var value in new[] { source.ServiceName, source.MachineName, source.PodName, source.ContainerName, source.Namespace, source.NodeName })
        {
            if (!string.IsNullOrWhiteSpace(value))
                builder.Append(" | ").Append(value);
        }

        if (source.Health != ConsoleLogSourceHealth.Connected)
            builder.Append(" | ").Append(source.Health);

        return builder.ToString();
    }

    private async Task ActivateAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadSourcesAsync(cancellationToken);
            var recent = await ConsoleLogService.GetRecentAsync(ViewState.Filter, ViewState.VisibleRowCap, cancellationToken);
            BackendDroppedCount = recent.DroppedLineCount ?? 0;

            if (recent.Sources is { Count: > 0 })
                MergeSources(recent.Sources);

            foreach (var line in recent.Items.OrderBy(x => x.Timestamp))
                ViewState.AddVisibleLine(line);

            await Observer.StartAsync(ViewState.Filter, cancellationToken);
            await ScrollToBottomAsync();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            ErrorMessage = null;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Unauthorized;
            Snackbar.Add(StateMessage ?? "You do not have permission to view diagnostics console logs.", Severity.Error);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ErrorMessage = e.Message;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Error;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshFilterAsync()
    {
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        var filter = CopyFilter(ViewState.Filter);
        IsLoading = true;
        _isRefreshing = true;
        _refreshBufferedLines.Clear();
        ErrorMessage = null;
        UpdateUrlFromState();

        try
        {
            var recent = await ConsoleLogService.GetRecentAsync(filter, ViewState.VisibleRowCap, _cancellationTokenSource.Token);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            ViewState.ClearVisibleRows();
            BackendDroppedCount = recent.DroppedLineCount ?? 0;

            if (recent.Sources is { Count: > 0 })
                MergeSources(recent.Sources);

            foreach (var line in recent.Items.OrderBy(x => x.Timestamp))
                ViewState.AddVisibleLine(line);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            await Observer.UpdateFilterAsync(filter, _cancellationTokenSource.Token);

            if (!IsCurrentRefresh(refreshVersion))
                return;

            FlushRefreshBufferedLines();
            _isRefreshing = false;
            await ScrollToBottomAsync();
        }
        catch (Exception e) when (IsAuthorizationFailure(e))
        {
            if (!IsCurrentRefresh(refreshVersion))
                return;

            ErrorMessage = null;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Unauthorized;
            Snackbar.Add(StateMessage ?? "You do not have permission to view diagnostics console logs.", Severity.Error);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (!IsCurrentRefresh(refreshVersion))
                return;

            ErrorMessage = e.Message;
            ViewState.ConnectionStatus = ConsoleLogConnectionStatus.Error;
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            if (IsCurrentRefresh(refreshVersion))
            {
                _isRefreshing = false;
                _refreshBufferedLines.Clear();
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await ConsoleLogService.ListSourcesAsync(cancellationToken);
        _sources.Clear();
        MergeSources(sources);
    }

    private Task OnLineReceivedAsync(ConsoleLogLine line)
    {
        return InvokeAsync(async () =>
        {
            if (_isRefreshing)
                _refreshBufferedLines.Add(line);
            else
                ViewState.AddIncomingLine(line);

            StateHasChanged();

            if (!_isRefreshing && !ViewState.IsPaused && ViewState.FollowTail)
                await ScrollToBottomAsync();
        });
    }

    private Task OnDroppedLinesReceivedAsync(ConsoleLogDroppedLineSummary summary)
    {
        return InvokeAsync(() =>
        {
            BackendDroppedCount += summary.DroppedLineCount;
            StateHasChanged();
        });
    }

    private Task OnConnectionStatusChangedAsync(ConsoleLogConnectionStatus status)
    {
        return InvokeAsync(() =>
        {
            ViewState.ConnectionStatus = status;
            StateHasChanged();
        });
    }

    private Task OnSourceChangedAsync(ConsoleLogSource source)
    {
        return InvokeAsync(() =>
        {
            MergeSources([source]);
            StateHasChanged();
        });
    }

    private void MergeSources(IEnumerable<ConsoleLogSource> sources)
    {
        foreach (var source in sources)
        {
            var index = _sources.FindIndex(x => string.Equals(x.Id, source.Id, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                _sources[index] = source;
            else
                _sources.Add(source);
        }

        _sources.Sort((left, right) => string.Compare(SourceDisplayName(left), SourceDisplayName(right), StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyQueryFromUrl()
    {
        if (_queryInitialized)
            return;

        UrlStateMapper.ApplyQuery(ViewState, NavigationManager.ToAbsoluteUri(NavigationManager.Uri));
        _fromFilterText = ViewState.Filter.From?.ToString("O");
        _toFilterText = ViewState.Filter.To?.ToString("O");
        _queryInitialized = true;
    }

    private void UpdateUrlFromState()
    {
        if (!_queryInitialized)
            return;

        var uri = NavigationManager.GetUriWithQueryParameters(UrlStateMapper.ToQueryParameters(ViewState));

        if (!string.Equals(uri, NavigationManager.Uri, StringComparison.Ordinal))
            NavigationManager.NavigateTo(uri, replace: true);
    }

    private async Task CopyTextAsync(string text, string successMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
            Snackbar.Add(successMessage, Severity.Success);
        }
        catch (JSException)
        {
            Snackbar.Add("Unable to copy. Check browser clipboard permissions.", Severity.Warning);
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
            // Scrolling is best-effort and should not interrupt console streaming.
        }
    }

    private async Task<IJSObjectReference> GetScriptModuleAsync()
    {
        _scriptModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Diagnostics.ConsoleLogs/consoleLogs.js");
        return _scriptModule;
    }

    private string GetStrippedText(ConsoleLogLine line)
    {
        if (!_strippedTextCache.TryGetValue(line, out var cachedText))
        {
            cachedText = new StrippedConsoleLogText(StripAnsi(line.Text));
            _strippedTextCache.Add(line, cachedText);
        }

        return cachedText.Text;
    }

    private void FlushRefreshBufferedLines()
    {
        if (_refreshBufferedLines.Count == 0)
            return;

        var visibleIds = Rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var line in _refreshBufferedLines)
        {
            if (!string.IsNullOrWhiteSpace(line.Id) && !visibleIds.Add(line.Id))
                continue;

            ViewState.AddIncomingLine(line);
        }

        _refreshBufferedLines.Clear();
    }

    private static string? NormalizeFilterText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool IsCurrentRefresh(long refreshVersion) => Interlocked.Read(ref _refreshVersion) == refreshVersion;

    private static bool IsAuthorizationFailure(Exception e)
    {
        return e switch
        {
            ApiException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => true,
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => true,
            _ => false
        };
    }

    private static ConsoleLogFilter CopyFilter(ConsoleLogFilter filter) => new()
    {
        SourceId = filter.SourceId,
        Streams = [.. filter.Streams ?? []],
        Text = filter.Text,
        From = filter.From,
        To = filter.To,
        Take = filter.Take
    };

    private static bool TryParseDateTimeOffset(string? value, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed);
    }

    private sealed class StrippedConsoleLogText(string text)
    {
        public string Text { get; } = text;
    }

    protected static string StripAnsi(string text)
    {
        var builder = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];

            if (character != '\u001b')
            {
                builder.Append(character);
                continue;
            }

            if (i + 1 >= text.Length)
                break;

            var introducer = text[++i];

            if (introducer == '[')
            {
                while (i + 1 < text.Length)
                {
                    var next = text[++i];

                    if (next is >= '@' and <= '~')
                        break;
                }

                continue;
            }

            if (introducer == ']')
            {
                while (i + 1 < text.Length)
                {
                    var next = text[++i];

                    if (next == '\a')
                        break;

                    if (next == '\u001b' && i + 1 < text.Length && text[i + 1] == '\\')
                    {
                        i++;
                        break;
                    }
                }

                continue;
            }

            if (introducer is 'P' or '^' or '_' or 'X')
            {
                while (i + 1 < text.Length)
                {
                    var next = text[++i];

                    if (next == '\u001b' && i + 1 < text.Length && text[i + 1] == '\\')
                    {
                        i++;
                        break;
                    }
                }
            }
        }

        return builder.ToString();
    }
}
