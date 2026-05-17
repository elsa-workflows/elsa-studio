using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogViewStateTests
{
    [Fact]
    public void AddVisibleLine_PrunesRowsPastLocalCap()
    {
        var state = new ConsoleLogViewState { VisibleRowCap = 2 };

        state.AddVisibleLine(Line("1"));
        state.AddVisibleLine(Line("2"));
        state.AddVisibleLine(Line("3"));

        Assert.Equal(["2", "3"], state.VisibleRows.Select(x => x.Id));
        Assert.Equal(1, state.DiscardedLocalRows);
    }

    [Fact]
    public void AddVisibleLine_WhenPaused_IncrementsPendingCount()
    {
        var state = new ConsoleLogViewState { IsPaused = true };

        state.AddVisibleLine(Line("1"));

        Assert.Empty(state.VisibleRows);
        Assert.Equal(1, state.PendingLineCount);
    }

    [Fact]
    public void ClearVisibleRows_RemovesLocalRowsAndCounts()
    {
        var state = new ConsoleLogViewState { VisibleRowCap = 1 };
        state.AddVisibleLine(Line("1"));
        state.AddVisibleLine(Line("2"));

        state.ClearVisibleRows();

        Assert.Empty(state.VisibleRows);
        Assert.Equal(0, state.DiscardedLocalRows);
        Assert.Equal(0, state.PendingLineCount);
    }

    private static ConsoleLogLine Line(string id) => new()
    {
        Id = id,
        Timestamp = DateTimeOffset.UtcNow,
        Source = new() { Id = "source-a" },
        Text = id
    };
}
