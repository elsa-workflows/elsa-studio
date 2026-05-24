using Xunit;
using ConsoleLogViewerComponent = Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components.ConsoleLogViewer;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogsPageHelperTests
{
    [Fact]
    public void StripAnsi_RemovesCsiSequences()
    {
        var text = TestConsoleLogs.StripAnsiForTest("plain \u001b[31mred\u001b[0m text");

        Assert.Equal("plain red text", text);
    }

    [Fact]
    public void StripAnsi_RemovesNonSgrCsiSequences()
    {
        var text = TestConsoleLogs.StripAnsiForTest("before\u001b[2K after");

        Assert.Equal("before after", text);
    }

    [Fact]
    public void CreateExportFileName_UsesTimestamp()
    {
        var fileName = TestConsoleLogs.CreateExportFileNameForTest(DateTimeOffset.Parse("2026-05-18T09:10:11Z"));

        Assert.Equal("diagnostics-console-logs-20260518-091011.tsv", fileName);
    }

    private class TestConsoleLogs : ConsoleLogViewerComponent
    {
        public static string StripAnsiForTest(string text) => StripAnsi(text);

        public static string CreateExportFileNameForTest(DateTimeOffset timestamp) => CreateExportFileName(timestamp);
    }
}
