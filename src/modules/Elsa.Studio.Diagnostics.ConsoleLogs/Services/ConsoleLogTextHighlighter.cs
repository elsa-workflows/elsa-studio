using Microsoft.AspNetCore.Components;
using System.Net;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Highlights text matches without mutating the underlying raw line text.
/// </summary>
public class ConsoleLogTextHighlighter
{
    /// <summary>
    /// Returns safe markup with matching text wrapped in mark elements.
    /// </summary>
    public MarkupString Highlight(string text, string? filter)
    {
        if (string.IsNullOrEmpty(text))
            return new("");

        if (string.IsNullOrWhiteSpace(filter))
            return new(WebUtility.HtmlEncode(text));

        var index = text.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return new(WebUtility.HtmlEncode(text));

        var parts = new List<string>();
        var current = 0;

        while (index >= 0)
        {
            parts.Add(WebUtility.HtmlEncode(text[current..index]));
            parts.Add($"<mark>{WebUtility.HtmlEncode(text.Substring(index, filter.Length))}</mark>");
            current = index + filter.Length;
            index = text.IndexOf(filter, current, StringComparison.OrdinalIgnoreCase);
        }

        parts.Add(WebUtility.HtmlEncode(text[current..]));
        return new(string.Concat(parts));
    }
}
