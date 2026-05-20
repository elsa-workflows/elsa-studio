using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogTextHighlighterTests
{
    private readonly ConsoleLogTextHighlighter _highlighter = new();

    [Fact]
    public void Highlight_WrapsMatches()
    {
        var markup = _highlighter.Highlight("one needle two NEEDLE", "needle");

        Assert.Contains("<mark>needle</mark>", markup.Value);
        Assert.Contains("<mark>NEEDLE</mark>", markup.Value);
    }

    [Fact]
    public void Highlight_EscapesRawText()
    {
        var markup = _highlighter.Highlight("<script>needle</script>", "needle");

        Assert.Contains("&lt;script&gt;", markup.Value);
        Assert.DoesNotContain("<script>", markup.Value);
    }
}
