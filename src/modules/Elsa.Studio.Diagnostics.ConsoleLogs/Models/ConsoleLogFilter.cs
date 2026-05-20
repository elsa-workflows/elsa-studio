namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Filter state shared by recent REST requests, live SignalR subscriptions, and URL query state.
/// </summary>
public class ConsoleLogFilter
{
    /// <summary>
    /// Gets or sets the stable backend source ID.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the selected streams.
    /// </summary>
    public ICollection<ConsoleLogStream>? Streams { get; set; } = [ConsoleLogStream.Stdout, ConsoleLogStream.Stderr];

    /// <summary>
    /// Gets or sets the server-side text filter.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the inclusive UTC start time.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Gets or sets the inclusive UTC end time.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Gets or sets the requested line count.
    /// </summary>
    public int? Take { get; set; }
}
