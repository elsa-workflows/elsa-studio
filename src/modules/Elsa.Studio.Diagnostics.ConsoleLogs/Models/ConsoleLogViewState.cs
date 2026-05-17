namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Local UI state for the diagnostics console viewer.
/// </summary>
public class ConsoleLogViewState
{
    private readonly List<ConsoleLogLine> _visibleRows = new();

    /// <summary>
    /// Gets the current filter.
    /// </summary>
    public ConsoleLogFilter Filter { get; } = new();

    /// <summary>
    /// Gets the bounded local visible rows.
    /// </summary>
    public IReadOnlyList<ConsoleLogLine> VisibleRows => _visibleRows;

    /// <summary>
    /// Gets or sets the maximum number of local visible rows.
    /// </summary>
    public int VisibleRowCap { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the count of rows discarded because of the local cap.
    /// </summary>
    public long DiscardedLocalRows { get; set; }

    /// <summary>
    /// Gets or sets the current connection status.
    /// </summary>
    public ConsoleLogConnectionStatus ConnectionStatus { get; set; } = ConsoleLogConnectionStatus.Disconnected;

    /// <summary>
    /// Gets or sets a value indicating whether live rendering is paused.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the number of lines waiting while paused.
    /// </summary>
    public long PendingLineCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the viewer follows the latest line.
    /// </summary>
    public bool FollowTail { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether lines wrap.
    /// </summary>
    public bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compact density is enabled.
    /// </summary>
    public bool Compact { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ANSI styling is displayed.
    /// </summary>
    public bool Ansi { get; set; } = true;

    /// <summary>
    /// Adds a visible line and prunes older rows according to the local cap.
    /// </summary>
    public void AddVisibleLine(ConsoleLogLine line)
    {
        if (IsPaused)
        {
            PendingLineCount++;
            return;
        }

        _visibleRows.Add(line);

        while (_visibleRows.Count > VisibleRowCap)
        {
            _visibleRows.RemoveAt(0);
            DiscardedLocalRows++;
        }
    }

    /// <summary>
    /// Clears local visible rows without changing backend capture.
    /// </summary>
    public void ClearVisibleRows()
    {
        _visibleRows.Clear();
        PendingLineCount = 0;
        DiscardedLocalRows = 0;
    }
}
