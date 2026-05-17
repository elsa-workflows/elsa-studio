namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Represents recent console line results.
/// </summary>
public class RecentConsoleLinesResult
{
    /// <summary>
    /// Gets or sets the recent console lines.
    /// </summary>
    public ICollection<ConsoleLogLine> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the backend dropped-line count.
    /// </summary>
    public long? DroppedLineCount { get; set; }

    /// <summary>
    /// Gets or sets optional source metadata.
    /// </summary>
    public ICollection<ConsoleLogSource>? Sources { get; set; }
}
