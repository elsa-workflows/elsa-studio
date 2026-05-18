using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Maps diagnostics console URL query state.
/// </summary>
public class ConsoleLogUrlStateMapper
{
    /// <summary>
    /// Applies canonical query parameters to the given view state.
    /// </summary>
    public void ApplyQuery(ConsoleLogViewState state, Uri uri)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("source", out var source))
            state.Filter.SourceId = Normalize(source);

        if (query.TryGetValue("stream", out var stream))
            state.Filter.Streams = ParseStreams(stream);

        if (query.TryGetValue("text", out var text))
            state.Filter.Text = Normalize(text);

        if (query.TryGetValue("from", out var from) && TryParseUtc(from, out var parsedFrom))
            state.Filter.From = parsedFrom;

        if (query.TryGetValue("to", out var to) && TryParseUtc(to, out var parsedTo))
            state.Filter.To = parsedTo;

        if (query.TryGetValue("wrap", out var wrap) && bool.TryParse(wrap, out var parsedWrap))
            state.Wrap = parsedWrap;

        if (query.TryGetValue("compact", out var compact) && bool.TryParse(compact, out var parsedCompact))
            state.Compact = parsedCompact;

        if (query.TryGetValue("ansi", out var ansi) && bool.TryParse(ansi, out var parsedAnsi))
            state.Ansi = parsedAnsi;

        if (query.TryGetValue("follow", out var follow) && bool.TryParse(follow, out var parsedFollow))
            state.FollowTail = parsedFollow;
    }

    /// <summary>
    /// Creates canonical query parameters for the given state.
    /// </summary>
    public Dictionary<string, object?> ToQueryParameters(ConsoleLogViewState state) =>
        new()
        {
            ["source"] = state.Filter.SourceId,
            ["stream"] = FormatStreams(state.Filter.Streams),
            ["text"] = state.Filter.Text,
            ["from"] = state.Filter.From?.ToUniversalTime().ToString("O"),
            ["to"] = state.Filter.To?.ToUniversalTime().ToString("O"),
            ["wrap"] = FormatBool(state.Wrap),
            ["compact"] = FormatBool(state.Compact),
            ["ansi"] = FormatBool(state.Ansi),
            ["follow"] = FormatBool(state.FollowTail)
        };

    /// <summary>
    /// Parses stream selection values.
    /// </summary>
    public static ICollection<ConsoleLogStream> ParseStreams(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "stdout" => [ConsoleLogStream.Stdout],
            "stderr" => [ConsoleLogStream.Stderr],
            _ => [ConsoleLogStream.Stdout, ConsoleLogStream.Stderr]
        };
    }

    /// <summary>
    /// Formats selected streams.
    /// </summary>
    public static string FormatStreams(ICollection<ConsoleLogStream>? streams)
    {
        if (streams == null || streams.Count != 1)
            return "both";

        return streams.Contains(ConsoleLogStream.Stderr) ? "stderr" : "stdout";
    }

    private static bool TryParseUtc(string? value, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out result);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatBool(bool value) => value ? "true" : "false";
}
