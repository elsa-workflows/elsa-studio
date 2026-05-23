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
        var uri = new Uri("https://studio/diagnostics/console?source=source-a&stream=stderr&wrap=true&compact=false&ansi=false&follow=false");

        _mapper.ApplyQuery(state, uri);

        Assert.Equal("source-a", state.Filter.SourceId);
        Assert.Equal(ConsoleLogStream.Stderr, state.Filter.Stream);
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

        Assert.Null(state.Filter.Stream);
    }

    [Fact]
    public void ToQueryParameters_UsesCanonicalNames()
    {
        var state = new ConsoleLogViewState { Wrap = true, Compact = false, Ansi = false, FollowTail = true };
        state.Filter.SourceId = "source-a";
        state.Filter.Stream = ConsoleLogStream.Stdout;

        var parameters = _mapper.ToQueryParameters(state);

        Assert.Equal("source-a", parameters["source"]);
        Assert.Equal("stdout", parameters["stream"]);
        Assert.Null(parameters["text"]);
        Assert.Null(parameters["from"]);
        Assert.Null(parameters["to"]);
        Assert.Equal("true", parameters["wrap"]);
        Assert.Equal("false", parameters["compact"]);
        Assert.Equal("false", parameters["ansi"]);
        Assert.Equal("true", parameters["follow"]);
    }
}
