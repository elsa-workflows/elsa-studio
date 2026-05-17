using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogUrlStateMapperTests
{
    private readonly ConsoleLogUrlStateMapper _mapper = new();

    [Fact]
    public void ApplyQuery_ParsesCanonicalParameters()
    {
        var state = new ConsoleLogViewState();
        var uri = new Uri("https://studio/diagnostics/console?source=source-a&stream=stderr&text=needle&from=2026-05-18T09:00:00Z&to=2026-05-18T10:00:00Z&wrap=true&compact=false&ansi=false&follow=false");

        _mapper.ApplyQuery(state, uri);

        Assert.Equal("source-a", state.Filter.SourceId);
        Assert.Equal([ConsoleLogStream.Stderr], state.Filter.Streams);
        Assert.Equal("needle", state.Filter.Text);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T09:00:00Z"), state.Filter.From);
        Assert.Equal(DateTimeOffset.Parse("2026-05-18T10:00:00Z"), state.Filter.To);
        Assert.True(state.Wrap);
        Assert.False(state.Compact);
        Assert.False(state.Ansi);
        Assert.False(state.FollowTail);
    }

    [Fact]
    public void ApplyQuery_InvalidStreamDefaultsToBoth()
    {
        var state = new ConsoleLogViewState();

        _mapper.ApplyQuery(state, new("https://studio/diagnostics/console?stream=nope"));

        Assert.Equal([ConsoleLogStream.Stdout, ConsoleLogStream.Stderr], state.Filter.Streams);
    }

    [Fact]
    public void ToQueryParameters_UsesCanonicalNames()
    {
        var state = new ConsoleLogViewState { Wrap = true, Compact = false, Ansi = false, FollowTail = true };
        state.Filter.SourceId = "source-a";
        state.Filter.Streams = [ConsoleLogStream.Stdout];
        state.Filter.Text = "needle";

        var parameters = _mapper.ToQueryParameters(state);

        Assert.Equal("source-a", parameters["source"]);
        Assert.Equal("stdout", parameters["stream"]);
        Assert.Equal("needle", parameters["text"]);
        Assert.True((bool)parameters["wrap"]!);
        Assert.False((bool)parameters["compact"]!);
        Assert.False((bool)parameters["ansi"]!);
        Assert.True((bool)parameters["follow"]!);
    }
}
