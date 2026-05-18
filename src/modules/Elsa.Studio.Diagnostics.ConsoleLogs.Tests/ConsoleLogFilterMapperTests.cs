using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogFilterMapperTests
{
    private readonly DateTimeOffset _from = DateTimeOffset.UtcNow.AddMinutes(-10);
    private readonly DateTimeOffset _to = DateTimeOffset.UtcNow;
    private readonly ConsoleLogFilter _filter;

    public ConsoleLogFilterMapperTests()
    {
        _filter = new()
        {
            SourceId = "source-a",
            Streams = [ConsoleLogStream.Stdout],
            Text = "incident",
            From = _from,
            To = _to,
            Take = 50
        };
    }

    [Fact]
    public void ToRecentRequest_MapsFilterFields()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(_filter, 100);

        Assert.Equal(_filter.SourceId, request.SourceId);
        Assert.Equal(_filter.Streams, request.Streams);
        Assert.Equal(_filter.Text, request.Text);
        Assert.Equal(_from, request.From);
        Assert.Equal(_to, request.To);
        Assert.Equal(50, request.Take);
    }

    [Fact]
    public void ToRecentRequest_ClampsTakeToRowCap()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(new() { Take = 500 }, 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToRecentRequest_DefaultsTakeToRowCap()
    {
        var request = ConsoleLogFilterMapper.ToRecentRequest(new(), 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToLiveSubscription_CopiesStreams()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(_filter);

        Assert.NotSame(_filter.Streams, request.Streams);
        Assert.Equal(_filter.Streams, request.Streams);
    }

    [Fact]
    public void ToLiveSubscription_PreservesRawTextFilter()
    {
        var request = ConsoleLogFilterMapper.ToLiveSubscription(new() { Text = " raw value " });

        Assert.Equal(" raw value ", request.Text);
    }
}
