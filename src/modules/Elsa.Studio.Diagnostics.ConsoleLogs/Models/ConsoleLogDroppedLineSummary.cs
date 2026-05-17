namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Describes backend-dropped console lines.
/// </summary>
public class ConsoleLogDroppedLineSummary
{
    /// <summary>
    /// Gets or sets the affected source ID.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the dropped-line count.
    /// </summary>
    public long DroppedLineCount { get; set; }

    /// <summary>
    /// Gets or sets the optional reason.
    /// </summary>
    public string? Reason { get; set; }
}
