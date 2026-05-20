namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Represents one raw stdout or stderr console line.
/// </summary>
public class ConsoleLogLine
{
    /// <summary>
    /// Gets or sets the stable line identity.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the backend line timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the backend receive timestamp.
    /// </summary>
    public DateTimeOffset? ReceivedAt { get; set; }

    /// <summary>
    /// Gets or sets the line sequence.
    /// </summary>
    public long? Sequence { get; set; }

    /// <summary>
    /// Gets or sets the process stream.
    /// </summary>
    public ConsoleLogStream Stream { get; set; }

    /// <summary>
    /// Gets or sets the raw console text.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets the source identity.
    /// </summary>
    public ConsoleLogSource Source { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the backend truncated this line.
    /// </summary>
    public bool IsTruncated { get; set; }

    /// <summary>
    /// Gets or sets the number of backend-dropped lines before this line.
    /// </summary>
    public long? DroppedBeforeCount { get; set; }
}
